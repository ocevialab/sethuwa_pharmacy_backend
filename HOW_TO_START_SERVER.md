# How to Start the API Server
## Getting Your Server Running

---

## 🚨 **The Problem**

**ERR_CONNECTION_REFUSED** means the server is **not running**.

You need to start the API server before accessing Swagger!

---

## 🚀 **How to Start the Server**

### **Method 1: Using Visual Studio / VS Code (Recommended)**

1. **Open the project** in Visual Studio or VS Code
2. **Press F5** or click the **Run/Debug** button
3. Select **"IIS Express"** or **"https"** profile
4. The server will start automatically

---

### **Method 2: Using dotnet CLI (Command Line)**

Open PowerShell or Command Prompt in the project directory and run:

```powershell
dotnet run
```

Or specify a profile:

```powershell
# HTTP only (port 5000)
dotnet run --launch-profile http

# HTTPS (ports 5000 and 5001)
dotnet run --launch-profile https

# IIS Express (ports 57470 and 44311)
dotnet run --launch-profile "IIS Express"
```

---

## 📍 **URLs Based on How You Start**

### **If using `dotnet run` (default):**
- **HTTP:** `http://localhost:5000/swagger`
- **HTTPS:** `https://localhost:5001/swagger`

### **If using IIS Express:**
- **HTTP:** `http://localhost:57470/swagger`
- **HTTPS:** `https://localhost:44311/swagger`

---

## ✅ **How to Check if Server is Running**

### **Check Port 5000 (dotnet run):**
```powershell
netstat -ano | findstr ":5000"
```

### **Check Port 57470 (IIS Express):**
```powershell
netstat -ano | findstr ":57470"
```

### **Check Port 44311 (IIS Express HTTPS):**
```powershell
netstat -ano | findstr ":44311"
```

**If you see output, the server is running!**

---

## 🔍 **Verify Server is Running**

When the server starts successfully, you'll see output like:

```
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
Application started. Press Ctrl+C to shut down.
```

---

## 🎯 **Quick Start Steps**

1. **Open terminal** in the project folder:
   ```powershell
   cd "C:\Users\iworld\Desktop\Ocevia Lab\Pharmacy Management System\pharmacy-management-system-API\pharamay_management_system_backend"
   ```

2. **Start the server:**
   ```powershell
   dotnet run
   ```

3. **Wait for this message:**
   ```
   Now listening on: http://localhost:5000
   Now listening on: https://localhost:5001
   Application started.
   ```

4. **Open browser and go to:**
   ```
   http://localhost:5000/swagger
   ```

---

## 🛑 **How to Stop the Server**

Press **Ctrl+C** in the terminal where the server is running.

---

## ⚠️ **Common Issues**

### **Issue 1: Port Already in Use**

**Error:** `Failed to bind to address http://localhost:5000: address already in use`

**Solution:**
1. Find and stop the process using the port:
   ```powershell
   netstat -ano | findstr ":5000"
   # Note the PID (last number)
   taskkill /PID <PID> /F
   ```
2. Or use a different port by modifying `launchSettings.json`

### **Issue 2: SQL Server Not Running**

**Error:** Database connection errors

**Solution:**
1. Make sure SQL Server Express is running
2. Check connection string in `appsettings.json`

### **Issue 3: Missing Dependencies**

**Error:** `dotnet run` fails with errors

**Solution:**
```powershell
dotnet restore
dotnet build
dotnet run
```

---

## 📋 **Summary**

| Action | Command | Result |
|--------|---------|--------|
| Start server | `dotnet run` | Runs on ports 5000 (HTTP) and 5001 (HTTPS) |
| Start with HTTP only | `dotnet run --launch-profile http` | Runs on port 5000 only |
| Start with HTTPS | `dotnet run --launch-profile https` | Runs on ports 5000 and 5001 |
| Check if running | `netstat -ano \| findstr ":5000"` | Shows if port is in use |
| Stop server | `Ctrl+C` | Stops the server |

---

## ✅ **After Starting Server**

Once the server is running, access Swagger at:

**For dotnet run:**
- `http://localhost:5000/swagger` ✅
- `https://localhost:5001/swagger`

**For IIS Express:**
- `http://localhost:57470/swagger` ✅
- `https://localhost:44311/swagger`

---

**Remember:** The server must be running before you can access Swagger!

