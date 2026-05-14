using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pharmacyPOS.API.DTOs;
using pharmacyPOS.API.Models;

namespace pharmacyPOS.API.Services;

public class MedicineExcelBulkUpdateService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MedicineExcelBulkUpdateService> _logger;

    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["medicine id"] = "medicineid",
        ["medicineid"] = "medicineid",
        ["med id"] = "medicineid",
        ["id"] = "medicineid",
        ["medicine name"] = "medicinename",
        ["medicinename"] = "medicinename",
        ["name"] = "medicinename",
        ["brand name"] = "brandname",
        ["brandname"] = "brandname",
        ["generic name"] = "genericname",
        ["genericname"] = "genericname",
        ["threshold"] = "threshold",
        ["low stock threshold"] = "threshold",
        ["lowstockthreshold"] = "threshold",
        ["barcode"] = "barcode",
        ["selling price"] = "sellingprice",
        ["sellingprice"] = "sellingprice",
        ["cost price"] = "costprice",
        ["costprice"] = "costprice",
        ["quantity"] = "quantity",
        ["qty"] = "quantity",
        ["quantity on hand"] = "quantity",
    };

    public MedicineExcelBulkUpdateService(
        IServiceScopeFactory scopeFactory,
        ILogger<MedicineExcelBulkUpdateService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<MedicineExcelBulkUpdateSummaryDto> ImportAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        var summary = new MedicineExcelBulkUpdateSummaryDto();

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null)
        {
            summary.Rows.Add(new MedicineExcelBulkUpdateRowResultDto
            {
                RowNumber = 0,
                Status = "Error",
                Message = "The workbook has no worksheets."
            });
            summary.ErrorCount = 1;
            return summary;
        }

        var firstRowUsed = ws.FirstRowUsed();
        if (firstRowUsed == null)
        {
            summary.Rows.Add(new MedicineExcelBulkUpdateRowResultDto
            {
                RowNumber = 0,
                Status = "Error",
                Message = "The sheet is empty."
            });
            summary.ErrorCount = 1;
            return summary;
        }

        var headerRowNum = firstRowUsed.RowNumber();
        var headerRow = ws.Row(headerRowNum);
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var raw = cell.GetString();
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var normalized = NormalizeHeader(raw);
            if (HeaderAliases.TryGetValue(normalized, out var key))
                colMap[key] = cell.Address.ColumnNumber;
        }

        if (!colMap.ContainsKey("medicineid"))
        {
            summary.Rows.Add(new MedicineExcelBulkUpdateRowResultDto
            {
                RowNumber = headerRowNum,
                Status = "Error",
                Message = "Missing required column: \"Medicine ID\" (or equivalent)."
            });
            summary.ErrorCount = 1;
            return summary;
        }

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRowNum;
        for (var r = headerRowNum + 1; r <= lastRow; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var medicineIdCell = ws.Row(r).Cell(colMap["medicineid"]);
            var medicineId = medicineIdCell.GetString().Trim();
            if (string.IsNullOrEmpty(medicineId))
            {
                summary.SkippedCount++;
                summary.Rows.Add(new MedicineExcelBulkUpdateRowResultDto
                {
                    RowNumber = r,
                    Status = "Skipped",
                    Message = "Empty Medicine ID."
                });
                continue;
            }

            summary.TotalRows++;
            var rowResult = await ProcessOneRowAsync(ws, r, colMap, medicineId, cancellationToken);
            summary.Rows.Add(rowResult);
            if (rowResult.Status == "Success")
                summary.SuccessCount++;
            else if (rowResult.Status == "Error")
                summary.ErrorCount++;
            else
                summary.SkippedCount++;
        }

        return summary;
    }

    private async Task<MedicineExcelBulkUpdateRowResultDto> ProcessOneRowAsync(
        IXLWorksheet ws,
        int rowNumber,
        IReadOnlyDictionary<string, int> colMap,
        string medicineId,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SethsuwaPharmacyDbContext>();

        try
        {
            var medicine = await db.Medicines.FirstOrDefaultAsync(
                m => m.MedicineId == medicineId, cancellationToken);

            if (medicine != null && medicine.IsDeleted)
            {
                return new MedicineExcelBulkUpdateRowResultDto
                {
                    RowNumber = rowNumber,
                    MedicineId = medicineId,
                    Status = "Error",
                    Message = "Medicine is soft-deleted; restore it before importing."
                };
            }

            var createdNew = false;
            Product? product = null;

            if (medicine == null)
            {
                if (await db.Products.AnyAsync(p => p.ProductSku == medicineId, cancellationToken))
                {
                    return new MedicineExcelBulkUpdateRowResultDto
                    {
                        RowNumber = rowNumber,
                        MedicineId = medicineId,
                        Status = "Error",
                        Message =
                            $"Product SKU \"{medicineId}\" is already in use. Use a different Medicine ID or clean up the existing product row."
                    };
                }

                if (!colMap.TryGetValue("medicinename", out var nameColCreate))
                {
                    return new MedicineExcelBulkUpdateRowResultDto
                    {
                        RowNumber = rowNumber,
                        MedicineId = medicineId,
                        Status = "Error",
                        Message =
                            "Cannot create medicine: add a \"Medicine Name\" column with a value for new items."
                    };
                }

                var newName = ws.Row(rowNumber).Cell(nameColCreate).GetString().Trim();
                if (string.IsNullOrEmpty(newName))
                {
                    return new MedicineExcelBulkUpdateRowResultDto
                    {
                        RowNumber = rowNumber,
                        MedicineId = medicineId,
                        Status = "Error",
                        Message = "Cannot create medicine: \"Medicine Name\" must be filled in for new items."
                    };
                }

                if (await db.Medicines.AnyAsync(m => m.Name == newName && !m.IsDeleted, cancellationToken))
                {
                    return new MedicineExcelBulkUpdateRowResultDto
                    {
                        RowNumber = rowNumber,
                        MedicineId = medicineId,
                        Status = "Error",
                        Message = $"A medicine named \"{newName}\" already exists."
                    };
                }

                int? lowStock = null;
                if (colMap.TryGetValue("threshold", out var thColCreate))
                {
                    var thCell = ws.Row(rowNumber).Cell(thColCreate);
                    if (!TryGetInt(thCell, out var thParsed))
                    {
                        return new MedicineExcelBulkUpdateRowResultDto
                        {
                            RowNumber = rowNumber,
                            MedicineId = medicineId,
                            Status = "Error",
                            Message = "Invalid \"Threshold\" value (expected a whole number)."
                        };
                    }

                    lowStock = thParsed;
                }

                string? brand = null;
                if (colMap.TryGetValue("brandname", out var bCol))
                {
                    var b = ws.Row(rowNumber).Cell(bCol).GetString().Trim();
                    brand = string.IsNullOrEmpty(b) ? null : b;
                }

                string? generic = null;
                if (colMap.TryGetValue("genericname", out var gCol))
                {
                    var g = ws.Row(rowNumber).Cell(gCol).GetString().Trim();
                    generic = string.IsNullOrEmpty(g) ? null : g;
                }

                medicine = new Medicine
                {
                    MedicineId = medicineId,
                    Name = newName,
                    BrandName = brand,
                    GenericName = generic,
                    Manufacture = null,
                    Category = null,
                    Strength = null,
                    RequiredPrescription = false,
                    LowStockThreshold = lowStock,
                    IsDeleted = false
                };
                db.Medicines.Add(medicine);

                product = new Product
                {
                    ProductSku = medicineId,
                    ProductType = "Medicine",
                    MedicineId = medicineId,
                    GlossaryId = null,
                    IsDeleted = false
                };
                db.Products.Add(product);
                createdNew = true;
            }
            else
            {
                if (colMap.TryGetValue("medicinename", out var nameCol))
                {
                    var name = ws.Row(rowNumber).Cell(nameCol).GetString().Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (await db.Medicines.AnyAsync(
                                m => m.Name == name && m.MedicineId != medicineId && !m.IsDeleted,
                                cancellationToken))
                        {
                            return new MedicineExcelBulkUpdateRowResultDto
                            {
                                RowNumber = rowNumber,
                                MedicineId = medicineId,
                                Status = "Error",
                                Message = $"Another medicine already uses the name \"{name}\"."
                            };
                        }

                        medicine.Name = name;
                    }
                }

                if (colMap.TryGetValue("brandname", out var brandCol))
                {
                    var v = ws.Row(rowNumber).Cell(brandCol).GetString().Trim();
                    medicine.BrandName = string.IsNullOrEmpty(v) ? null : v;
                }

                if (colMap.TryGetValue("genericname", out var genCol))
                {
                    var v = ws.Row(rowNumber).Cell(genCol).GetString().Trim();
                    medicine.GenericName = string.IsNullOrEmpty(v) ? null : v;
                }

                if (colMap.TryGetValue("threshold", out var thCol))
                {
                    var cell = ws.Row(rowNumber).Cell(thCol);
                    if (!TryGetInt(cell, out var threshold))
                    {
                        return new MedicineExcelBulkUpdateRowResultDto
                        {
                            RowNumber = rowNumber,
                            MedicineId = medicineId,
                            Status = "Error",
                            Message = "Invalid \"Threshold\" value (expected a whole number)."
                        };
                    }

                    medicine.LowStockThreshold = threshold;
                }
            }

            if (!createdNew)
            {
                product = await db.Products.FirstOrDefaultAsync(
                    p => p.MedicineId == medicineId && p.ProductType == "Medicine" && !p.IsDeleted,
                    cancellationToken);

                if (product == null)
                {
                    if (await db.Products.AnyAsync(p => p.ProductSku == medicineId, cancellationToken))
                    {
                        return new MedicineExcelBulkUpdateRowResultDto
                        {
                            RowNumber = rowNumber,
                            MedicineId = medicineId,
                            Status = "Error",
                            Message =
                                $"Cannot attach product: SKU \"{medicineId}\" already exists on another product row."
                        };
                    }

                    product = new Product
                    {
                        ProductSku = medicineId,
                        ProductType = "Medicine",
                        MedicineId = medicineId,
                        GlossaryId = null,
                        IsDeleted = false
                    };
                    db.Products.Add(product);
                }
            }

            var warnings = new List<string>();
            if (colMap.ContainsKey("barcode"))
                warnings.Add("Barcode column is ignored (not stored in the database).");

            var wantSelling = colMap.TryGetValue("sellingprice", out var spCol);
            var wantCost = colMap.TryGetValue("costprice", out var cpCol);
            var wantQty = colMap.TryGetValue("quantity", out var qCol);

            if (wantSelling || wantCost || wantQty)
            {
                var batches = await db.Stocks
                    .Where(s => s.ProductSku == product!.ProductSku)
                    .OrderBy(s => s.ExpireDate)
                    .ThenBy(s => s.StockId)
                    .ToListAsync(cancellationToken);

                if (batches.Count == 0)
                {
                    warnings.Add("No stock batches found; selling price, cost price, and quantity were not updated.");
                }
                else
                {
                    var target = batches[0];
                    if (batches.Count > 1)
                        warnings.Add(
                            "Multiple stock batches: prices and quantity were applied only to the earliest-expiry batch (FEFO first).");

                    if (wantSelling)
                    {
                        var cell = ws.Row(rowNumber).Cell(spCol);
                        if (!TryGetDecimal(cell, out var sp))
                        {
                            return new MedicineExcelBulkUpdateRowResultDto
                            {
                                RowNumber = rowNumber,
                                MedicineId = medicineId,
                                Status = "Error",
                                Message = "Invalid \"Selling Price\" value."
                            };
                        }

                        target.SellingPrice = sp;
                    }

                    if (wantCost)
                    {
                        var cell = ws.Row(rowNumber).Cell(cpCol);
                        if (!TryGetDecimal(cell, out var cp))
                        {
                            return new MedicineExcelBulkUpdateRowResultDto
                            {
                                RowNumber = rowNumber,
                                MedicineId = medicineId,
                                Status = "Error",
                                Message = "Invalid \"Cost Price\" value."
                            };
                        }

                        target.CostPrice = cp;
                    }

                    if (wantQty)
                    {
                        var cell = ws.Row(rowNumber).Cell(qCol);
                        if (!TryGetInt(cell, out var qty) || qty < 0)
                        {
                            return new MedicineExcelBulkUpdateRowResultDto
                            {
                                RowNumber = rowNumber,
                                MedicineId = medicineId,
                                Status = "Error",
                                Message = "Invalid \"Quantity\" value (expected a non‑negative whole number)."
                            };
                        }

                        target.QuantityOnHand = qty;
                    }
                }
            }

            await using (var tx = await db.Database.BeginTransactionAsync(cancellationToken))
            {
                await db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }

            var msg = createdNew ? "Created." : "Updated.";
            if (warnings.Count > 0)
                msg += " " + string.Join(" ", warnings);

            return new MedicineExcelBulkUpdateRowResultDto
            {
                RowNumber = rowNumber,
                MedicineId = medicineId,
                Status = "Success",
                Message = msg
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel bulk row failed for {MedicineId} at row {Row}", medicineId, rowNumber);
            return new MedicineExcelBulkUpdateRowResultDto
            {
                RowNumber = rowNumber,
                MedicineId = medicineId,
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    private static string NormalizeHeader(string raw) =>
        Regex.Replace(raw.Trim(), @"\s+", " ", RegexOptions.CultureInvariant);

    private static bool TryGetDecimal(IXLCell cell, out decimal value)
    {
        value = 0;
        if (cell.IsEmpty())
            return false;

        if (cell.DataType == XLDataType.Number)
        {
            value = Convert.ToDecimal(cell.GetDouble(), CultureInfo.InvariantCulture);
            return true;
        }

        var s = cell.GetString().Trim();
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
               || decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
    }

    private static bool TryGetInt(IXLCell cell, out int value)
    {
        value = 0;
        if (cell.IsEmpty())
            return false;

        if (cell.DataType == XLDataType.Number)
        {
            var d = cell.GetDouble();
            if (d is >= int.MinValue and <= int.MaxValue && Math.Abs(d - Math.Round(d)) < 0.0001)
            {
                value = (int)Math.Round(d);
                return true;
            }

            return false;
        }

        var s = cell.GetString().Trim();
        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
               || int.TryParse(s, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
    }
}
