# Swagger Authorization Fix
## How to Use JWT Authentication in Swagger UI

---

## 🐛 **The Problem**

Getting **401 Unauthorized** even after clicking "Authorize" in Swagger.

The curl command shows the token is missing the "Bearer " prefix:
```bash
-H 'Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```

Should be:
```bash
-H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```

---

## ✅ **The Fix Applied**

Updated Swagger configuration to use `SecuritySchemeType.Http` instead of `ApiKey`, which properly handles Bearer token authentication.

---

## 🚀 **How to Use (After Fix)**

### **Step 1: Get Your Token**

1. Use the `/api/Auth/login` endpoint in Swagger
2. Click "Try it out"
3. Enter your credentials:
   ```json
   {
     "employeeId": "EMP-1",
     "password": "your-password"
   }
   ```
4. Click "Execute"
5. Copy the `token` value from the response (the long string starting with `eyJ...`)

### **Step 2: Authorize in Swagger**

1. Click the **Authorize** button (🔒) at the top right of Swagger UI
2. In the "Value" field, enter **ONLY the token** (without "Bearer " prefix):
   ```
   eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJFbXBsb3llZUlkIjoiRU1QLTEi...
   ```
3. **Do NOT** include "Bearer " - Swagger will add it automatically now!
4. Click "Authorize"
5. Click "Close"

### **Step 3: Test Protected Endpoints**

Now try any protected endpoint (like `/api/Customer`) - it should work!

---

## 🔍 **What Changed**

### **Before (Incorrect):**
- Used `SecuritySchemeType.ApiKey`
- Token format was not handled correctly
- Required manual "Bearer " prefix

### **After (Fixed):**
- Uses `SecuritySchemeType.Http` with `Scheme = "Bearer"`
- Swagger automatically adds "Bearer " prefix
- Just paste the token directly

---

## 📋 **Quick Reference**

| Step | Action | What to Enter |
|------|--------|---------------|
| 1 | Login | `{"employeeId": "EMP-1", "password": "..."}` |
| 2 | Copy token | Copy the `token` value from response |
| 3 | Click Authorize | 🔒 button at top right |
| 4 | Paste token | Paste token **only** (no "Bearer " prefix) |
| 5 | Click Authorize | Confirm authorization |
| 6 | Test endpoint | Try `/api/Customer` or any protected endpoint |

---

## ✅ **After Restart**

1. **Restart your API server** (stop and start again to apply the fix)
2. **Refresh Swagger UI** in your browser
3. **Login** to get a fresh token
4. **Authorize** with just the token (no "Bearer " prefix)
5. **Test** your endpoints!

---

**Status:** ✅ **FIXED** - Restart server and use the new authorization method!

