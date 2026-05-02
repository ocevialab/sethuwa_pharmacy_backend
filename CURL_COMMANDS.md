# Correct curl Commands - No Certificate Warnings
## Using HTTP for Localhost Development

---

## ✅ **Fully Correct Solution (No Certificate Warnings)**

Since your API runs on both HTTP and HTTPS, use **HTTP** for localhost development to avoid SSL certificate issues.

### **Your API Endpoints:**
- **HTTP:** `http://localhost:57470` (No certificate needed!)
- **HTTPS:** `https://localhost:44311` (Requires certificate verification)

---

## 🚀 **Correct curl Commands**

### **1. Login (Get Token)**

```bash
curl -X POST 'http://localhost:57470/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{
    "employeeId": "EMP-1",
    "password": "your-password"
  }'
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "employeeId": "EMP-1",
  "employeeName": "Thilanka Perera",
  "role": "OWNER"
}
```

---

### **2. Get All Customers (Protected Endpoint)**

```bash
curl -X GET 'http://localhost:57470/api/Customer' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE'
```

**Replace `YOUR_TOKEN_HERE`** with the actual token from login response.

---

### **3. Get Customer by ID**

```bash
curl -X GET 'http://localhost:57470/api/Customer/CUS-001' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE'
```

---

### **4. Create Customer**

```bash
curl -X POST 'http://localhost:57470/api/Customer' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE' \
  -d '{
    "customerName": "John Doe",
    "contactNumber": "0771234567",
    "emailAddress": "john@example.com",
    "address": "123 Main St",
    "discount": 5.0,
    "customerStatus": "Active"
  }'
```

---

### **5. Update Customer**

```bash
curl -X PUT 'http://localhost:57470/api/Customer/CUS-001' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE' \
  -d '{
    "customerName": "John Doe Updated",
    "contactNumber": "0771234567",
    "emailAddress": "john.updated@example.com",
    "address": "456 New St",
    "discount": 10.0,
    "customerStatus": "Active"
  }'
```

---

### **6. Search Customers**

```bash
curl -X GET 'http://localhost:57470/api/Customer/search?query=John' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE'
```

---

### **7. Delete Customer (Soft Delete)**

```bash
curl -X DELETE 'http://localhost:57470/api/Customer/CUS-001' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE'
```

---

## 📝 **Complete Workflow Example**

### **Step 1: Login and Save Token**

```bash
# Login and save response to variable (PowerShell)
$response = curl -X POST 'http://localhost:57470/api/Auth/login' `
  -H 'Content-Type: application/json' `
  -d '{"employeeId": "EMP-1", "password": "your-password"}' | ConvertFrom-Json

$token = $response.token
Write-Host "Token: $token"
```

### **Step 2: Use Token in Requests**

```bash
# Use the saved token
curl -X GET "http://localhost:57470/api/Customer" `
  -H "Authorization: Bearer $token"
```

---

## 🔄 **Using HTTP vs HTTPS**

### **For Development (localhost):**
✅ **Use HTTP:** `http://localhost:57470`
- No certificate warnings
- Simpler and faster
- Perfectly safe for local development

### **For Production:**
✅ **Use HTTPS:** `https://yourdomain.com`
- SSL certificate properly configured
- Secure encrypted connections
- Required for production

---

## 💻 **Platform-Specific Commands**

### **Windows PowerShell:**

```powershell
# Single line
curl.exe -X GET 'http://localhost:57470/api/Customer' -H 'Authorization: Bearer YOUR_TOKEN'

# Multi-line (using backtick)
curl.exe -X GET 'http://localhost:57470/api/Customer' `
  -H 'accept: */*' `
  -H 'Authorization: Bearer YOUR_TOKEN'
```

### **Windows CMD:**

```cmd
curl -X GET "http://localhost:57470/api/Customer" ^
  -H "accept: */*" ^
  -H "Authorization: Bearer YOUR_TOKEN"
```

### **Linux/Mac/Bash:**

```bash
curl -X GET 'http://localhost:57470/api/Customer' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer YOUR_TOKEN'
```

---

## ✅ **Key Differences from HTTPS**

| Aspect | HTTPS (with -k) | HTTP (Recommended) |
|--------|----------------|-------------------|
| URL | `https://localhost:44311` | `http://localhost:57470` |
| Certificate Warning | ❌ Shows warning | ✅ No warning |
| Flag Needed | `-k` (insecure) | ✅ None needed |
| Clean Output | ❌ Mixed with warnings | ✅ Clean output |
| Development Use | ⚠️ Not recommended | ✅ Recommended |

---

## 🎯 **Your Exact Working Command**

Replace `YOUR_TOKEN_HERE` with your actual token:

```bash
curl -X GET 'http://localhost:57470/api/Customer' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE'
```

**No `-k` flag needed!**  
**No certificate warnings!**  
**Fully correct and clean!** ✅

---

## 📋 **Quick Reference**

- **Base URL:** `http://localhost:57470`
- **Login:** `POST /api/Auth/login`
- **Get Customers:** `GET /api/Customer` (Requires: OWNER, ADMIN)
- **Get Customer:** `GET /api/Customer/{id}` (Requires: OWNER, ADMIN, CASHIER, DISPENSER)
- **Create Customer:** `POST /api/Customer` (Requires: OWNER, ADMIN, CASHIER, DISPENSER)
- **Update Customer:** `PUT /api/Customer/{id}` (Requires: OWNER, ADMIN)
- **Delete Customer:** `DELETE /api/Customer/{id}` (Requires: OWNER, ADMIN)
- **Search:** `GET /api/Customer/search?query=...` (Requires: OWNER, ADMIN, CASHIER, DISPENSER)

---

## 🔐 **Token Format**

Always use:
```
Authorization: Bearer <your-token>
```

**Remember:**
- ✅ Include "Bearer" (capital B)
- ✅ Include one space after "Bearer"
- ✅ Then your token
- ✅ No quotes around the token value

---

**Status:** ✅ **Fully Correct - No Warnings!**

Use HTTP (`http://localhost:57470`) for clean, warning-free curl commands!

