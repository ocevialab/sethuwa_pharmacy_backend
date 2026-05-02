# JWT 401 Error Troubleshooting Guide
## Still Getting 401 After Adding "Bearer" Prefix?

---

## 🔍 **Common Causes & Solutions**

### **1. Token Expired**

Even if you just logged in, check if the token has expired.

**Check Token Expiration:**
- Decode your JWT token at https://jwt.io
- Look for the `exp` field (expiration timestamp)
- Compare with current time

**Solution:** Login again to get a fresh token

---

### **2. Clock Skew / System Time Mismatch**

If your system clock is off, token validation can fail.

**Check:**
```powershell
# Verify system time is correct
Get-Date
```

**Solution:** Sync your system clock or ensure server/client times are synchronized

---

### **3. Token Format Issues**

Make sure the Authorization header is **exactly**:

```
Authorization: Bearer <token>
```

**Common Mistakes:**
- ❌ `Authorization: bearer token` (lowercase)
- ❌ `Authorization: Bearertoken` (no space)
- ❌ `Authorization: Bearer  token` (double space)
- ❌ `Authorization: token` (missing Bearer)

**Correct:**
- ✅ `Authorization: Bearer <token>` (exactly one space after "Bearer")

---

### **4. Token Extraction Error**

If using the token from login response, make sure you're extracting just the token value, not the whole response.

**Wrong:**
```javascript
const response = await fetch('/api/Auth/login', ...);
const token = response; // Wrong!
```

**Correct:**
```javascript
const response = await fetch('/api/Auth/login', ...);
const data = await response.json();
const token = data.token; // Extract the token property
```

---

### **5. curl Command Format**

Make sure your curl command has proper quoting:

**Windows PowerShell:**
```powershell
curl.exe -X GET `
  'https://localhost:44311/api/Customer' `
  -H 'accept: */*' `
  -H 'Authorization: Bearer YOUR_TOKEN_HERE'
```

**Windows CMD:**
```cmd
curl -X GET ^
  "https://localhost:44311/api/Customer" ^
  -H "accept: */*" ^
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Linux/Mac/Bash:**
```bash
curl -X GET \
  'https://localhost:44311/api/Customer' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE'
```

---

### **6. HTTPS Certificate Issue**

If using `https://localhost`, you might need to bypass certificate validation.

**For curl:**
```bash
curl -k -X GET 'https://localhost:44311/api/Customer' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE'
```

The `-k` flag tells curl to ignore SSL certificate errors (for localhost development only!)

---

### **7. Using Stale/Old Token**

If you copied the token from an earlier session, it might be expired or invalid.

**Solution:** 
1. Login again to get a fresh token
2. Use that new token immediately

---

### **8. Role/Permission Issue**

Check if your token has the required role for the endpoint.

**Your endpoint requires:** `OWNER` or `ADMIN` role

**Check your token at https://jwt.io:**
- Look for the `role` claim
- It should be: `"http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "OWNER"`

---

### **9. Server Configuration Mismatch**

The token was created with certain issuer/audience values, and the server validates against different values.

**Token must have:**
- `iss` (issuer): `pharmacypos.api`
- `aud` (audience): `pharmacypos.client`

**Verify in appsettings.json:**
```json
{
  "Jwt": {
    "Issuer": "pharmacypos.api",
    "Audience": "pharmacypos.client"
  }
}
```

---

## 🧪 **Step-by-Step Debugging**

### **Step 1: Get a Fresh Token**

```bash
curl -X POST 'https://localhost:44311/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"employeeId": "EMP-1", "password": "your-password"}' \
  -k
```

**Copy the `token` value from the response**

### **Step 2: Test with the Fresh Token**

```bash
# Replace YOUR_TOKEN_HERE with the actual token
curl -X GET 'https://localhost:44311/api/Customer' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE' \
  -k \
  -v
```

The `-v` flag shows verbose output including response headers.

### **Step 3: Check Server Logs**

Look at the console output or log files for JWT authentication error messages.

The server will now log:
- `JWT Authentication failed: <error message>`
- `JWT Challenge error: <error>`

### **Step 4: Decode and Verify Token**

1. Go to https://jwt.io
2. Paste your token
3. Verify:
   - Token has 3 parts (header.payload.signature)
   - `exp` (expiration) is in the future
   - `iss` matches `pharmacypos.api`
   - `aud` matches `pharmacypos.client`
   - Role claim exists

---

## 🔍 **Verify Your curl Command**

Make sure your exact command looks like this:

```bash
curl -X GET 'https://localhost:44311/api/Customer' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJFbXBsb3llZUlkIjoiRU1QLTEiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJPV05FUiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJUaGlsYW5rYSBQZXJlcmEiLCJTdGF0dXMiOiJBY3RpdmUiLCJleHAiOjE3NjYwOTMwNTEsImlzcyI6InBoYXJtYWN5cG9zLmFwaSIsImF1ZCI6InBoYXJtYWN5cG9zLmNsaWVudCJ9.WcupE7zgwOIUNOJyKPb7xUUrPwxMjUgV1qGQgXgFL4M' \
  -k
```

**Key points:**
- ✅ `Authorization: Bearer` (with space)
- ✅ Full token after "Bearer "
- ✅ `-k` flag to bypass SSL certificate validation (for localhost)

---

## 📋 **Quick Checklist**

- [ ] Got a fresh token by logging in again
- [ ] Using `Authorization: Bearer <token>` format (exactly)
- [ ] Token is not expired (check `exp` claim)
- [ ] System clock is correct
- [ ] Using correct endpoint URL
- [ ] Token has required role (OWNER or ADMIN)
- [ ] Server is running and accessible
- [ ] Checked server logs/console for error messages

---

## 🆘 **If Still Not Working**

1. **Check server console/logs** - Look for the JWT authentication error messages we added
2. **Test with Swagger UI** - Use `/swagger` to test with the authorize button (handles formatting automatically)
3. **Compare working vs non-working** - If Swagger works but curl doesn't, it's a formatting issue
4. **Verify token at jwt.io** - Make sure token structure is valid
5. **Try a different endpoint** - Test with a simpler endpoint first

---

## 📝 **Exact Command to Try**

Replace `YOUR_TOKEN` with a fresh token from login:

```bash
curl -k -X GET 'https://localhost:44311/api/Customer' -H 'Authorization: Bearer YOUR_TOKEN' -v
```

The `-v` flag will show you exactly what's happening and what error you're getting.

---

**Next Steps:** 
1. Get a fresh token
2. Try the exact command above
3. Check server console for error messages
4. Share the error message if it still fails

