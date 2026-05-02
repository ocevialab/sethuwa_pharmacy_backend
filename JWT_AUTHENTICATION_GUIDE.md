# JWT Authentication Guide
## How to Use JWT Tokens with the API

---

## 🐛 **The Problem**

You're getting a **401 Unauthorized** error because the JWT token is not being sent in the correct format.

### **Your Current Request:**
```bash
curl -X 'GET' \
  'https://localhost:44311/api/Customer' \
  -H 'Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```

### **The Issue:**
The `Authorization` header is missing the **"Bearer "** prefix!

---

## ✅ **The Solution**

JWT tokens must be sent with the **"Bearer "** prefix in the Authorization header.

### **Correct Format:**
```bash
Authorization: Bearer <your-token-here>
```

### **Fixed curl Command:**
```bash
curl -X 'GET' \
  'https://localhost:44311/api/Customer' \
  -H 'accept: */*' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJFbXBsb3llZUlkIjoiRU1QLTEiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJPV05FUiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJUaGlsYW5rYSBQZXJlcmEiLCJTdGF0dXMiOiJBY3RpdmUiLCJleHAiOjE3NjYwOTMwNTEsImlzcyI6InBoYXJtYWN5cG9zLmFwaSIsImF1ZCI6InBoYXJtYWN5cG9zLmNsaWVudCJ9.WcupE7zgwOIUNOJyKPb7xUUrPwxMjUgV1qGQgXgFL4M'
```

**Notice:** `Bearer ` (with a space after "Bearer") before the token!

---

## 📚 **Understanding JWT Bearer Authentication**

### **What is Bearer Token Authentication?**

Bearer token authentication is a method where:
1. You receive a JWT token after successful login
2. You include that token in **every** protected API request
3. The token must be prefixed with "Bearer " in the Authorization header
4. The server validates the token and extracts user information

### **The Authorization Header Format:**

```
Authorization: Bearer <jwt-token>
```

**Components:**
- `Authorization:` - Header name
- `Bearer` - Authentication scheme (tells the server how to interpret the token)
- `<jwt-token>` - Your actual JWT token

---

## 🔍 **How It Works in Your API**

### **1. Login to Get Token**

```bash
curl -X 'POST' \
  'https://localhost:44311/api/Auth/login' \
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

### **2. Use Token in Subsequent Requests**

Extract the `token` value and use it in the Authorization header:

```bash
curl -X 'GET' \
  'https://localhost:44311/api/Customer' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```

---

## 💻 **Examples in Different Tools**

### **Using curl:**

```bash
# GET request
curl -X 'GET' \
  'https://localhost:44311/api/Customer' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE'

# POST request
curl -X 'POST' \
  'https://localhost:44311/api/Customer' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_TOKEN_HERE' \
  -d '{
    "customerName": "John Doe",
    "contactNumber": "0771234567"
  }'
```

### **Using JavaScript/Fetch:**

```javascript
const token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';

fetch('https://localhost:44311/api/Customer', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
})
.then(response => response.json())
.then(data => console.log(data));
```

### **Using Axios:**

```javascript
import axios from 'axios';

const token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';

axios.get('https://localhost:44311/api/Customer', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
})
.then(response => console.log(response.data));
```

### **Using Postman:**

1. Go to the **Authorization** tab
2. Select **Bearer Token** from the Type dropdown
3. Paste your token in the Token field
4. Postman will automatically format it as `Bearer <token>`

### **Using Swagger UI:**

1. Click the **Authorize** button (🔒) at the top
2. Enter your token (without "Bearer " prefix - Swagger adds it automatically)
3. Click **Authorize**
4. All requests will now include the token

---

## 🔐 **Authorization Requirements**

Your API endpoints have role-based authorization:

### **Customer Controller Endpoints:**

| Endpoint | Method | Required Roles |
|----------|--------|----------------|
| `/api/Customer` | GET | `OWNER`, `ADMIN` |
| `/api/Customer/{id}` | GET | `OWNER`, `ADMIN`, `CASHIER`, `DISPENSER` |
| `/api/Customer` | POST | `OWNER`, `ADMIN`, `CASHIER`, `DISPENSER` |
| `/api/Customer/{id}` | PUT | `OWNER`, `ADMIN` |
| `/api/Customer/{id}` | DELETE | `OWNER`, `ADMIN` |
| `/api/Customer/search` | GET | `OWNER`, `ADMIN`, `CASHIER`, `DISPENSER` |

### **Your Token Contains:**
Based on your token, you have the role: **`OWNER`**

This means you have access to **all** endpoints! ✅

---

## ⚠️ **Common Mistakes**

### **❌ Mistake 1: Missing "Bearer " prefix**
```bash
-H 'Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```
**Result:** 401 Unauthorized

### **✅ Correct:**
```bash
-H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```

---

### **❌ Mistake 2: Extra spaces or typos**
```bash
-H 'Authorization: bearer token'  # lowercase "bearer"
-H 'Authorization: Bearertoken'   # missing space
-H 'Authorization: Bearer  token' # double space
```

### **✅ Correct:**
```bash
-H 'Authorization: Bearer token'  # exactly "Bearer " (with space)
```

---

### **❌ Mistake 3: Using wrong header name**
```bash
-H 'Auth: Bearer token'           # wrong header name
-H 'Token: Bearer token'          # wrong header name
```

### **✅ Correct:**
```bash
-H 'Authorization: Bearer token'  # must be "Authorization"
```

---

### **❌ Mistake 4: Expired token**
Tokens expire after the time specified in `Jwt:ExpiresInMinutes` (currently 1440 minutes = 24 hours).

**Solution:** Login again to get a new token.

---

### **❌ Mistake 5: Token not extracted from response**
```javascript
// Wrong - using entire response
const response = await fetch('/api/Auth/login', {...});
fetch('/api/Customer', {
  headers: { 'Authorization': `Bearer ${response}` }
});

// Correct - extract token property
const response = await fetch('/api/Auth/login', {...});
const data = await response.json();
const token = data.token; // Extract token
fetch('/api/Customer', {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

---

## 🔍 **Debugging Authentication Issues**

### **1. Check Token Expiration**

Your token payload contains:
```json
{
  "exp": 1766093051,
  "iss": "pharmacypos.api",
  "aud": "pharmacypos.client"
}
```

- `exp` is the expiration timestamp (Unix time)
- Check if current time < expiration time

### **2. Verify Token Format**

A valid JWT has 3 parts separated by dots:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJFbXBsb3llZUlkIjoiRU1QLTEi...signature
  └─ Header ─┘ └─────────── Payload ───────────┘ └── Signature ──┘
```

### **3. Check Required Role**

Make sure your token contains the required role:
```json
{
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "OWNER"
}
```

### **4. Test with Swagger**

Use Swagger UI to test - it handles token formatting automatically:
1. Navigate to `/swagger`
2. Click **Authorize** (🔒)
3. Enter your token
4. Test the endpoint

---

## 📋 **Complete Example Workflow**

```bash
# Step 1: Login to get token
curl -X 'POST' 'https://localhost:44311/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"employeeId": "EMP-1", "password": "password123"}'

# Response:
# {"token": "eyJhbGci...", "employeeId": "EMP-1", ...}

# Step 2: Use token for protected endpoint
TOKEN="eyJhbGci..."

curl -X 'GET' 'https://localhost:44311/api/Customer' \
  -H 'Authorization: Bearer '"$TOKEN" \
  -H 'accept: application/json'
```

---

## ✅ **Quick Fix for Your Current Issue**

**Change this:**
```bash
-H 'Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```

**To this:**
```bash
-H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
```

**Remember:** Add `Bearer ` (with a space) before your token!

---

**Status:** ✅ **Fixed** - Use `Bearer ` prefix in Authorization header

