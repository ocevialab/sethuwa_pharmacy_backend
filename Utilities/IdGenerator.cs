using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace pharmacyPOS.API.Utilities
{
    public static class IdGenerator
    {
        /// <summary>
        /// Generates the next sequential ID for an entity using numeric extraction.
        /// This method is simple and reliable, handling IDs like "MED-1", "MED-2", ..., "MED-10" correctly.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="dbSet">The DbSet for the entity</param>
        /// <param name="prefix">The prefix for the ID (e.g., "MED", "GLO", "SUP")</param>
        /// <param name="idPropertyName">The name of the ID property</param>
        /// <param name="logger">Optional logger for debugging</param>
        /// <returns>The next sequential ID (e.g., "MED-11" if highest is "MED-10")</returns>
        public static async Task<string> GenerateNextSequentialId<TEntity>(
            this DbSet<TEntity> dbSet,
            string prefix,
            string idPropertyName,
            ILogger? logger = null)
            where TEntity : class
        {
            logger?.LogInformation(
                "Generating next sequential ID for entity {EntityName} with prefix '{Prefix}' on property '{IdPropertyName}'",
                typeof(TEntity).Name, prefix, idPropertyName);

            string fullPrefix = prefix + "-";
            int nextIdNumber = 1;

            try
            {
                // Get all existing IDs with the prefix
                var allIds = await dbSet
                    .Select(e => EF.Property<string>(e, idPropertyName))
                    .Where(id => id != null && id.StartsWith(fullPrefix))
                    .ToListAsync();

                logger?.LogDebug("Found {Count} existing IDs with prefix '{Prefix}'", allIds.Count, fullPrefix);

                if (allIds.Any())
                {
                    // Extract numeric parts and find the maximum
                    var maxNumber = allIds
                        .Select(id => id.Substring(fullPrefix.Length))
                        .Where(numStr => int.TryParse(numStr, out _))
                        .Select(numStr => int.Parse(numStr))
                        .DefaultIfEmpty(0)
                        .Max();

                    nextIdNumber = maxNumber + 1;
                    logger?.LogDebug("Found max number: {MaxNumber}, next ID number: {NextIdNumber}", maxNumber, nextIdNumber);
                }
                else
                {
                    logger?.LogDebug("No existing IDs found, starting from 1");
                }

                // Safety check: Verify the generated ID doesn't already exist
                // This handles race conditions where another request might have created the same ID
                string candidateId = $"{fullPrefix}{nextIdNumber}";
                int retryCount = 0;
                const int maxRetries = 5;

                while (retryCount < maxRetries)
                {
                    var exists = await dbSet
                        .Where(e => EF.Property<string>(e, idPropertyName) == candidateId)
                        .AnyAsync();

                    if (!exists)
                    {
                        // ID is available
                        break;
                    }

                    logger?.LogWarning("ID '{CandidateId}' already exists (attempt {RetryCount}/{MaxRetries}), trying next",
                        candidateId, retryCount + 1, maxRetries);

                    nextIdNumber++;
                    candidateId = $"{fullPrefix}{nextIdNumber}";
                    retryCount++;
                }

                if (retryCount >= maxRetries)
                {
                    logger?.LogError("Failed to find available ID after {MaxRetries} attempts", maxRetries);
                    throw new InvalidOperationException($"Unable to generate unique ID for prefix '{prefix}' after {maxRetries} attempts");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex,
                    "Error occurred while generating next sequential ID for prefix '{Prefix}'. Falling back to ID 1.",
                    prefix);

                // On error, try to find a safe ID by checking existing ones
                try
                {
                    var allIds = await dbSet
                        .Select(e => EF.Property<string>(e, idPropertyName))
                        .Where(id => id != null && id.StartsWith(fullPrefix))
                        .ToListAsync();

                    if (allIds.Any())
                    {
                        var maxNumber = allIds
                            .Select(id => id.Substring(fullPrefix.Length))
                            .Where(numStr => int.TryParse(numStr, out _))
                            .Select(numStr => int.Parse(numStr))
                            .DefaultIfEmpty(0)
                            .Max();

                        nextIdNumber = maxNumber + 1;
                        logger?.LogInformation("Recovered from error, using ID number: {NextIdNumber}", nextIdNumber);
                    }
                }
                catch
                {
                    // If recovery also fails, start from 1
                    nextIdNumber = 1;
                }
            }

            string newId = $"{fullPrefix}{nextIdNumber}";

            logger?.LogInformation("Generated new ID: {NewId}", newId);

            return newId;
        }
    }
}
