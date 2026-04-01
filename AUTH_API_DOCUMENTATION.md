### Secure Login API Documentation

## Overview
This document describes the role-based authentication system using JWT tokens implemented in the VDLCRM API.

## Role Structure
The system supports 4 roles:

| Role ID | Role Name | Description |
|---------|-----------|-------------|
| 1 | Admin | Full access to all resources |
| 2 | Internal User | Can view and manage student data |
| 3 | External User | Limited read-only access |
| 4 | Student | Access to personal student information |

## Database Schema

### Roles Table
```sql
CREATE TABLE roles (
    RoleSequenceId INTEGER PRIMARY KEY AUTOINCREMENT,
    RoleName TEXT NOT NULL,
    RoleId INTEGER NOT NULL UNIQUE
);
```

**Pre-seeded Data:**
- Admin (RoleId: 1)
- Internal User (RoleId: 2)
- External User (RoleId: 3)
- Student (RoleId: 4)

### Users Table
```sql
CREATE TABLE users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    Email TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    RoleId INTEGER NOT NULL,
    IsActive INTEGER NOT NULL,
    CreatedDate TEXT NOT NULL,
    UpdatedDate TEXT NOT NULL,
    FOREIGN KEY (RoleId) REFERENCES roles(RoleId)
);
```

## Authentication Endpoints

### 1. Login
**Endpoint:** `POST /api/auth/login`

**Request Body:**
```json
{
  "username": "admin",
  "password": "password123"
}
```

**Success Response (200):**
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

**Error Response (401):**
```json
{
  "success": false,
  "message": "Invalid username or password",
  "token": null,
  "user": null
}
```

### 2. Register
**Endpoint:** `POST /api/auth/register`

**Request Body:**
```json
{
  "username": "newuser",
  "email": "newuser@example.com",
  "password": "securePassword123!",
  "roleId": 4
}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "User registered successfully",
  "token": null,
  "user": {
    "id": 2,
    "username": "VDL001",
    "email": "newuser@example.com",
    "name": "newuser",
    "roleId": 4,
    "roleName": "Student",
    "isActive": true
  }
}
```

**Error Response (400):**
```json
{
  "success": false,
  "message": "Username or email already exists",
  "token": null,
  "user": null
}
```

### 3. Get All Roles
**Endpoint:** `GET /api/auth/roles`

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "roleSequenceId": 1,
      "roleName": "Admin",
      "roleId": 1
    },
    {
      "roleSequenceId": 2,
      "roleName": "Internal User",
      "roleId": 2
    },
    {
      "roleSequenceId": 3,
      "roleName": "External User",
      "roleId": 3
    },
    {
      "roleSequenceId": 4,
      "roleName": "Student",
      "roleId": 4
    }
  ]
}
```

## JWT Token Format

The JWT token contains the following claims:
- `sub` (NameIdentifier): User ID
- `unique_name` (Name): Username
- `email`: User email
- `RoleId`: Role ID (1-4)
- `RoleName`: Role name
- `role`: Role name (standard role claim)
- `exp`: Token expiration time
- `iss`: Issuer (VdlcrmApi)
- `aud`: Audience (VdlcrmUsers)

**Sample JWT Claims:**
```json
{
  "sub": "1",
  "unique_name": "admin",
  "email": "admin@example.com",
  "RoleId": "1",
  "RoleName": "Admin",
  "role": "Admin",
  "iss": "VdlcrmApi",
  "aud": "VdlcrmUsers",
  "exp": 1739107264
}
```

## Using JWT Token in API Requests

Include the token in the `Authorization` header as a Bearer token:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Example Request:**
```bash
curl -X GET "http://localhost:5000/api/student" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

## Role-Based Access Control

### Student Endpoints Protection

| Endpoint | Method | Required Role | Access |
|----------|--------|---------------|--------|
| `/api/student/register` | POST | None | Public (No authentication required) |
| `/api/StudentList` | GET | Admin, Internal User | Protected |
| `/api/student/{id}` | GET | Any Authenticated User | Protected |
| `/api/student/{id}` | PUT | Admin | Protected |
| `/api/student/{id}` | DELETE | Admin | Protected |

### Error Responses

**Unauthorized (401) - Missing or Invalid Token:**
```json
{
  "type": "http://schemas.microsoft.com/aspnet/core/identity/claims/invalidtoken",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid token"
}
```

**Forbidden (403) - Insufficient Permissions:**
```json
{
  "type": "http://schemas.microsoft.com/aspnet/core/mvc/modelvalidation",
  "title": "Forbidden",
  "status": 403,
  "detail": "Access forbidden. Required role: Admin"
}
```

## Configuration

### appsettings.json JWT Settings
```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long!!!!",
    "Issuer": "VdlcrmApi",
    "Audience": "VdlcrmUsers",
    "ExpirationMinutes": 60
  }
}
```

**Important:** Change the `SecretKey` to a strong, unique value in production.

## Testing with HTTP Client

### Using Student.http file

```http
### Login
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password123"
}

### Register User
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "testuser",
  "email": "testuser@example.com",
  "password": "Test123!@#",
  "roleId": 4
}

### Get All Roles
GET http://localhost:5000/api/auth/roles

### Get All Students (Admin/Internal User Only)
GET http://localhost:5000/api/StudentList
Authorization: Bearer <YOUR_JWT_TOKEN>

### Get Student by ID
GET http://localhost:5000/api/student/1
Authorization: Bearer <YOUR_JWT_TOKEN>

### Update Student (Admin Only)
PUT http://localhost:5000/api/student/1
Authorization: Bearer <YOUR_JWT_TOKEN>
Content-Type: application/json

{
  "id": 1,
  "vdlId": "VDL001",
  "name": "John Doe",
  "email": "john@example.com",
  ...
}

### Delete Student (Admin Only)
DELETE http://localhost:5000/api/student/1
Authorization: Bearer <YOUR_JWT_TOKEN>
```

## Security Best Practices

1. **Secret Key Management**: Never commit the secret key to version control. Use environment variables or secure vaults.
2. **HTTPS**: Always use HTTPS in production.
3. **Token Expiration**: Tokens expire after the configured time (default: 60 minutes). Implement token refresh mechanism for long-lived sessions.
4. **Password Security**: Passwords are hashed using BCrypt with strong hash values.
5. **Role Validation**: Always validate user roles on sensitive operations.

## Implementation Classes

### AuthService
- Handles user login and registration
- Generates JWT tokens
- Verifies passwords using BCrypt
- Manages role-based access

### DTOs
- `LoginRequest`: Username and password
- `LoginResponse`: Authentication result with token
- `RegisterRequest`: Registration form data
- `UserDto`: User information returned in responses

### DbContext Updates
- Added `Roles` DbSet
- Added `Users` DbSet
- Configured relationships and constraints

## Next Steps

1. Build and run migrations: `dotnet ef database update`
2. Create initial admin user via registration endpoint
3. Test login with admin credentials
4. Use returned token for protected endpoints
5. Implement token refresh mechanism (optional)
