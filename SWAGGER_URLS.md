# Swagger UI URLs
## Accessing Swagger Documentation

---

## 🌐 **Swagger UI URLs**

### **Using dotnet run (Most Common):**

**HTTP:**
```
http://localhost:5000/swagger
```

**HTTPS:**
```
https://localhost:5001/swagger
```

---

### **Using IIS Express:**

**HTTP:**
```
http://localhost:57470/swagger
```

**HTTPS:**
```
https://localhost:44311/swagger
```

---

## 🚀 **Recommended for Development**

**Use HTTP (No certificate warnings):**

**If using dotnet run:**
```
http://localhost:5000/swagger
```

**If using IIS Express:**
```
http://localhost:57470/swagger
```

---

## 📋 **Swagger Configuration**

- **Route Prefix:** `/swagger`
- **API Endpoint:** `/swagger/v1/swagger.json`
- **Only Available in:** Development environment

---

## 🔐 **How to Use Swagger with JWT Authentication**

1. **Open Swagger UI:**
   - Navigate to `http://localhost:57470/swagger`

2. **Login to Get Token:**
   - Find `/api/Auth/login` endpoint
   - Click "Try it out"
   - Enter your credentials:
     ```json
     {
       "employeeId": "EMP-1",
       "password": "your-password"
     }
     ```
   - Click "Execute"
   - Copy the `token` value from the response

3. **Authorize in Swagger:**
   - Click the **Authorize** button (🔒) at the top right
   - In the "Value" field, enter: `Bearer YOUR_TOKEN_HERE`
   - **Note:** Enter just the token, Swagger adds "Bearer " prefix automatically
   - Click "Authorize"
   - Click "Close"

4. **Test Protected Endpoints:**
   - All requests will now include the JWT token
   - Try any protected endpoint (like `/api/Customer`)

---

## ✅ **Quick Access**

**Bookmark this URL for easy access:**
```
http://localhost:57470/swagger
```

---

## 🔍 **Swagger Endpoints**

| Endpoint | Description |
|----------|-------------|
| `/swagger` | Swagger UI (Interactive API Documentation) |
| `/swagger/v1/swagger.json` | OpenAPI/Swagger JSON specification |

---

**Current Configuration:**
- Route Prefix: `swagger` (configured in Program.cs)
- Only enabled in Development environment
- JWT Bearer authentication is configured

