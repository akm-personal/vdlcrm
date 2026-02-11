# Quick Start Guide - VDLCRM Secure Authentication

## 🚀 Get Started in 5 Minutes

### Prerequisites
- .NET 10.0 SDK installed
- At least 5GB disk space
- Terminal/Command Prompt access

### Step 1: Build the Project
```bash
cd /workspaces/vdlcrm
dotnet build
```
**Expected:** ✅ Build succeeded

### Step 2: Run Database Migration
```bash
cd Vdlcrm.Web
dotnet ef database update --project ../Vdlcrm.Services/Vdlcrm.Services.csproj
```
**Expected:** Database updated successfully

### Step 3: Start the API
```bash
dotnet run
```
**Expected:** 
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Step 4: Register an Admin User

Open a new terminal and run:
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "email": "admin@example.com",
    "password": "Admin123!@#",
    "roleId": 1
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully"
}
```

### Step 5: Login and Get Token

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Admin123!@#"
  }'
```

**Response (save the token!):**
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@example.com",
    "roleId": 1,
    "roleName": "Admin",
    "isActive": true
  }
}
```

### Step 6: Access Protected Endpoint

```bash
TOKEN="<your-token-from-step-5>"

curl -X GET http://localhost:5000/api/student \
  -H "Authorization: Bearer $TOKEN"
```

**Response:** 
```json
[]  # Empty array if no students exist
```

✅ **Congratulations! Your authentication system is working!**

---

## 🔑 Key Features Implemented

| Feature | Details |
|---------|---------|
| **JWT Authentication** | Secure token-based authentication |
| **4 Roles** | Admin, Internal User, External User, Student |
| **Password Security** | BCrypt hashing with salt |
| **Role-Based Access** | Granular control per endpoint |
| **Token Claims** | User info + role info in JWT |
| **Database** | SQLite with Role & User tables |

---

## 📚 Next Steps

### 1. Test Different Roles
```bash
# Create Internal User
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"internaluser","email":"internal@example.com","password":"Pass123!@#","roleId":2}'

# Create External User
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"externaluser","email":"external@example.com","password":"Pass123!@#","roleId":3}'

# Create Student
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"student","email":"student@example.com","password":"Pass123!@#","roleId":4}'
```

### 2. Try Different Endpoints
```bash
# Get All Roles
curl -X GET http://localhost:5000/api/auth/roles \
  -H "Authorization: Bearer $TOKEN"

# Get specific student
curl -X GET http://localhost:5000/api/student/1 \
  -H "Authorization: Bearer $TOKEN"
```

### 3. Test Authorization
```bash
# Login as External User
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"externaluser","password":"Pass123!@#"}' | jq -r '.token')

# Try to get all students (FAILS - External User not allowed)
curl -X GET http://localhost:5000/api/student \
  -H "Authorization: Bearer $TOKEN"
# Returns 403 Forbidden
```

---

## 🔧 Configuration

### Change JWT Secret Key
**File:** `Vdlcrm.Web/appsettings.json`
```json
{
  "JwtSettings": {
    "SecretKey": "your-new-super-secret-key-at-least-32-chars!",
    "ExpirationMinutes": 120
  }
}
```

### Change Token Expiration
```json
{
  "JwtSettings": {
    "ExpirationMinutes": 120  # Change from 60 to 120
  }
}
```

### Database Location
**File:** `Vdlcrm.Web/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=vdlcrm.db"
  }
}
```

---

## 📖 Documentation Files

| File | Purpose |
|------|---------|
| [AUTH_API_DOCUMENTATION.md](AUTH_API_DOCUMENTATION.md) | Complete API reference |
| [API_TEST_EXAMPLES.md](API_TEST_EXAMPLES.md) | Request/response examples |
| [AUTHENTICATION_IMPLEMENTATION.md](AUTHENTICATION_IMPLEMENTATION.md) | Implementation details |
| [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) | Database setup instructions |
| [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md) | System architecture & flows |

---

## 🧪 Using Postman

### Import as Raw Text
Create a new request with these details:

**Login Request:**
```
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin123!@#"
}
```

**Get Students Request:**
```
GET http://localhost:5000/api/student
Authorization: Bearer <TOKEN_FROM_LOGIN>
```

### Use as Environment Variable
1. Copy token from login response
2. Set Postman environment variable: `{{token}}`
3. Use in header: `Authorization: Bearer {{token}}`

---

## 🐛 Troubleshooting

### "Database is locked"
```bash
# Kill any running processes
pkill -f "dotnet run"

# Try migration again
dotnet ef database update --project ../Vdlcrm.Services/Vdlcrm.Services.csproj
```

### "Invalid token" error
```bash
# Make sure:
1. Token is not expired (default: 60 minutes)
2. Token is in the Authorization header
3. Format is: Bearer <TOKEN>
4. Secret key in appsettings.json is unchanged
```

### "Forbidden" (403) on protected endpoint
```bash
# Check:
1. User has the required role
2. Endpoint has [Authorize] attribute
3. Role matches endpoint requirements
```

### Build fails
```bash
cd /workspaces/vdlcrm
dotnet clean
dotnet restore
dotnet build
```

---

## 📋 Common Commands

| Task | Command |
|------|---------|
| Build | `dotnet build` |
| Run | `dotnet run` |
| Test | See API_TEST_EXAMPLES.md |
| View DB | `sqlite3 vdlcrm.db` |
| Reset DB | `rm vdlcrm.db && dotnet ef database update` |
| View Logs | Check terminal output |

---

## 🔒 Security Checklist

Before Production:
- [ ] Change JWT secret key in appsettings.json
- [ ] Set `ExpirationMinutes` to appropriate value
- [ ] Enable HTTPS (uncomment in Program.cs)
- [ ] Use strong passwords for admin users
- [ ] Store secrets in environment variables
- [ ] Review role permissions
- [ ] Enable audit logging (optional)
- [ ] Set up backup strategy for SQLite DB

---

## 🎯 What's Included

✅ User authentication with JWT tokens  
✅ Role-based access control (4 roles)  
✅ Password hashing with BCrypt  
✅ Role and User database tables  
✅ Student endpoint protection  
✅ Error handling and validation  
✅ Configuration-driven settings  
✅ Full API documentation  
✅ Test examples  
✅ Architecture diagrams  

---

## 💡 Example Use Cases

### Admin Dashboard
```bash
# Admin can view all students
curl -H "Authorization: Bearer ADMIN_TOKEN" \
  http://localhost:5000/api/student
```

### Internal Staff Portal
```bash
# Internal users can view but not modify
curl -H "Authorization: Bearer INTERNAL_TOKEN" \
  http://localhost:5000/api/student
```

### External Partner Access
```bash
# External users can only view specific student by ID
curl -H "Authorization: Bearer EXTERNAL_TOKEN" \
  http://localhost:5000/api/student/1
```

### Student Self-Service
```bash
# Students can register without login
curl -X POST http://localhost:5000/api/student/register \
  -d '{"vdlId":"VDL001","name":"John"...}'
```

---

## 📞 Support

For detailed information, see the documentation files:
- **API Details**: AUTH_API_DOCUMENTATION.md
- **Testing Guide**: API_TEST_EXAMPLES.md
- **Setup Help**: MIGRATION_GUIDE.md
- **Architecture**: ARCHITECTURE_DIAGRAMS.md

---

## ✨ Key Points

1. **Token Format**: `Authorization: Bearer <JWT_TOKEN>`
2. **Token Duration**: Configurable, default 60 minutes
3. **Roles**: Admin (1), Internal (2), External (3), Student (4)
4. **Database**: SQLite with automatic migration
5. **Passwords**: BCrypt hashed (never stored in plain text)
6. **Endpoints**: Mix of public, authenticated, and role-protected

**You're all set! Happy coding! 🎉**
