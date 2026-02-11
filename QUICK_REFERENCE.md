# 🔑 Authentication System - Quick Reference

## Authentication Endpoints

### 1️⃣ Register New User (Public)
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "newuser",
  "email": "user@example.com",
  "password": "SecurePassword123!@#",
  "roleId": 4
}
```
**Roles:** 1=Admin, 2=Internal, 3=External, 4=Student

**Response 200:**
```json
{
  "success": true,
  "message": "User registered successfully"
}
```

---

### 2️⃣ Login (Public)
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "newuser",
  "password": "SecurePassword123!@#"
}
```

**Response 200:**
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "newuser",
    "email": "user@example.com",
    "roleId": 4,
    "roleName": "Student",
    "isActive": true
  }
}
```

---

### 3️⃣ Get All Roles (Protected)
```http
GET /api/auth/roles
Authorization: Bearer <TOKEN>
```

**Response 200:**
```json
{
  "success": true,
  "data": [
    {"roleSequenceId": 1, "roleName": "Admin", "roleId": 1},
    {"roleSequenceId": 2, "roleName": "Internal User", "roleId": 2},
    {"roleSequenceId": 3, "roleName": "External User", "roleId": 3},
    {"roleSequenceId": 4, "roleName": "Student", "roleId": 4}
  ]
}
```

---

## Student Endpoints (Protected)

### 4️⃣ Register Student (Public - No Auth)
```http
POST /api/student/register
Content-Type: application/json

{
  "vdlId": "VDL001",
  "name": "John Doe",
  "email": "student@example.com",
  "fatherName": "James Doe",
  "dateOfBirth": "2005-05-15",
  "gender": "Male",
  "address": "123 Main St",
  "mobileNumber": "9876543210",
  "alternateNumber": "9876543211",
  "class": "10A",
  "idProof": "Aadhar",
  "shiftType": "Morning",
  "seatNumber": 1,
  "studentStatus": "Active"
}
```

---

### 5️⃣ Get All Students (Admin, Internal User Only)
```http
GET /api/student
Authorization: Bearer <TOKEN>
```

**Response 200:**
```json
[
  {
    "id": 1,
    "vdlId": "VDL001",
    "name": "John Doe",
    "email": "student@example.com",
    "class": "10A",
    ...
  }
]
```

**Response 403 (External User, Student, Unauthenticated):**
```json
{
  "title": "Forbidden",
  "status": 403
}
```

---

### 6️⃣ Get Student by ID (All Authenticated Users)
```http
GET /api/student/1
Authorization: Bearer <TOKEN>
```

---

### 7️⃣ Update Student (Admin Only)
```http
PUT /api/student/1
Authorization: Bearer <TOKEN>
Content-Type: application/json

{
  "id": 1,
  "vdlId": "VDL001",
  "name": "John Doe Updated",
  ...
}
```

---

### 8️⃣ Delete Student (Admin Only)
```http
DELETE /api/student/1
Authorization: Bearer <TOKEN>
```

---

## JWT Token Usage

### Token Format
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOi...
```

### Token Claims
```json
{
  "sub": "1",                           // User ID
  "unique_name": "admin",               // Username
  "email": "admin@example.com",         // Email
  "RoleId": "1",                        // Role ID
  "RoleName": "Admin",                  // Role Name
  "role": "Admin",                      // Standard Role Claim
  "iss": "VdlcrmApi",                   // Issuer
  "aud": "VdlcrmUsers",                 // Audience
  "exp": 1739107264,                    // Expiration
  "iat": 1739103664                     // Issued At
}
```

### Token Lifespan
- Default: 60 minutes
- From login time
- Auto-expires after duration
- Cannot be renewed (new login required)

---

## Role Permissions Matrix

| Endpoint | Role 1 Admin | Role 2 Internal | Role 3 External | Role 4 Student |
|----------|:-:|:-:|:-:|:-:|
| **POST /student** | ✅ | ✅ | ❌ | ❌ |
| **GET /student (all)** | ✅ | ✅ | ❌ | ❌ |
| **GET /student/{id}** | ✅ | ✅ | ✅ | ✅ |
| **PUT /student/{id}** | ✅ | ❌ | ❌ | ❌ |
| **DELETE /student/{id}** | ✅ | ❌ | ❌ | ❌ |

---

## Error Responses

### 400 Bad Request
```json
{
  "message": "Invalid username or student details."
}
```

### 401 Unauthorized
```json
{
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid token"
}
```

### 403 Forbidden
```json
{
  "title": "Forbidden",
  "status": 403,
  "detail": "Access Denied"
}
```

### 404 Not Found
```json
{
  "message": "Student with ID 1 not found."
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred while processing your request.",
  "error": "Exception details"
}
```

---

## Test Commands (cURL)

### Register
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","email":"admin@ex.com","password":"Pass123!","roleId":1}'
```

### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Pass123!"}' | jq '.token'
```

### Get Students
```bash
TOKEN="<token_from_login>"
curl -X GET http://localhost:5000/api/student \
  -H "Authorization: Bearer $TOKEN"
```

### Get Roles
```bash
curl -X GET http://localhost:5000/api/auth/roles \
  -H "Authorization: Bearer $TOKEN"
```

---

## Database Information

### Connection String
```
Data Source=vdlcrm.db
```

### Tables
- **roles** - Role definitions
- **users** - User accounts
- **student_details** - Student records

### Roles Seed Data
```sql
INSERT INTO roles (RoleName, RoleId) VALUES
  ('Admin', 1),
  ('Internal User', 2),
  ('External User', 3),
  ('Student', 4);
```

---

## Configuration (appsettings.json)

```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long!!!",
    "Issuer": "VdlcrmApi",
    "Audience": "VdlcrmUsers",
    "ExpirationMinutes": 60
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=vdlcrm.db"
  }
}
```

---

## HTTP Status Codes Reference

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Request successful |
| 201 | Created | Resource created |
| 400 | Bad Request | Invalid input |
| 401 | Unauthorized | Missing/invalid token |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource doesn't exist |
| 500 | Server Error | Unexpected error |

---

## Authentication Flow

```
1. User Input → Credentials (username, password)
   ↓
2. POST /api/auth/login → Server validates
   ↓
3. Server checks:
   - Username exists
   - Password matches (BCrypt)
   - User is active
   ↓
4. Generate JWT Token with claims
   ↓
5. Return Token + User Info
   ↓
6. Client Stores Token (localStorage, session, etc)
   ↓
7. Subsequent Requests → Include in Authorization header
   ↓
8. Server Validates:
   - Token signature valid
   - Token not expired
   - User still active
   ↓
9. Check Endpoint Authorization:
   - Role requirement met?
   - Public endpoint?
   ↓
10. Process Request / Return Response
```

---

## Password Requirements

- ✅ BCrypt hashed (not plain text)
- ✅ Salted during hashing
- ✅ One-way encryption (cannot be reversed)
- ✅ No password reset needed for validation

---

## Common Issues & Solutions

### Issue: "Invalid token"
**Solution:** Token expired (60 min default), login again

### Issue: "Access Denied (403)"
**Solution:** Your role doesn't have permission, use Admin role

### Issue: "User not found (401)"
**Solution:** Username doesn't exist or incorrect password

### Issue: "Database locked"
**Solution:** Stop running process, then retry

### Issue: "Build error"
**Solution:** Run `dotnet restore && dotnet clean && dotnet build`

---

## Best Practices

✅ Always use HTTPS in production  
✅ Never commit secret key to version control  
✅ Change secret key from default  
✅ Use strong, unique passwords  
✅ Implement token refresh (optional)  
✅ Log authentication attempts  
✅ Monitor failed login attempts  
✅ Regularly rotate secrets  

---

## Testing Checklist

- [ ] Register user in each role
- [ ] Login with each role
- [ ] Access public endpoints (no token needed)
- [ ] Access protected endpoints (token required)
- [ ] Verify role-based access restrictions
- [ ] Test invalid credentials
- [ ] Test expired tokens
- [ ] Test insufficient permissions
- [ ] Test database connectivity
- [ ] Check error messages

---

## Related Documentation

- [QUICK_START.md](QUICK_START.md) - 5-minute setup
- [AUTH_API_DOCUMENTATION.md](AUTH_API_DOCUMENTATION.md) - Full API docs
- [API_TEST_EXAMPLES.md](API_TEST_EXAMPLES.md) - More examples
- [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) - Database setup
- [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md) - System design

---

**Last Updated:** February 9, 2026  
**Status:** ✅ Complete & Verified
