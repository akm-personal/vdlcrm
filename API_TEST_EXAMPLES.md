### Authentication API Test Examples

## Using REST Client / Postman

### 1. Register a New Admin User
```http
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "admin",
  "email": "admin@vdlcrm.com",
  "password": "AdminPassword123!@#",
  "roleId": 1
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "User registered successfully"
}
```

### 2. Register an Internal User
```http
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "internaluser",
  "email": "internal@vdlcrm.com",
  "password": "InternalPass123!@#",
  "roleId": 2
}
```

### 3. Register an External User
```http
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "externaluser",
  "email": "external@vdlcrm.com",
  "password": "ExternalPass123!@#",
  "roleId": 3
}
```

### 4. Login with Admin Credentials
```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "AdminPassword123!@#"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AdmRsY3JtLmNvbSIsIlJvbGVJZCI6IjEiLCJSb2xlTmFtZSI6IkFkbWluIiwicm9sZSI6IkFkbWluIiwiaWF0IjoxNzM5MDI4MDAwLCJleHAiOjE3MzkwMzE2MDAsImlzcyI6IlZkbGNybUFwaSIsImF1ZCI6IlZkbGNybVVzZXJzIn0.signature...",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@vdlcrm.com",
    "roleId": 1,
    "roleName": "Admin",
    "isActive": true
  }
}
```

**Save the token from the response for subsequent requests.**

### 5. Get All Available Roles
```http
GET http://localhost:5000/api/auth/roles
```

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

### 6. Retrieve All Students (Admin/Internal User Only)

**Replace TOKEN with your actual JWT token from login response**

```http
GET http://localhost:5000/api/student
Authorization: Bearer TOKEN

```

**Success Response (200):**
```json
[
  {
    "id": 1,
    "vdlId": "VDL001",
    "name": "John Doe",
    "email": "john@example.com",
    "fatherName": "James Doe",
    "dateOfBirth": "2005-05-15T00:00:00",
    "gender": "Male",
    "address": "123 Main St",
    "mobileNumber": "9876543210",
    "alternateNumber": "9876543211",
    "class": "10A",
    "idProof": "Aadhar",
    "shiftType": "Morning",
    "seatNumber": 1,
    "studentStatus": "Active",
    "createdDate": "2025-02-01T10:00:00",
    "updatedDate": "2025-02-01T10:00:00"
  }
]
```

**Error Response (403) - External User trying to access:**
```json
{
  "title": "Forbidden",
  "status": 403,
  "detail": "Access forbidden"
}
```

### 7. Register a Student (Public Endpoint - No Authentication)
```http
POST http://localhost:5000/api/student/register
Content-Type: application/json

{
  "vdlId": "VDL002",
  "name": "Jane Smith",
  "email": "jane@example.com",
  "fatherName": "Robert Smith",
  "dateOfBirth": "2006-07-20T00:00:00",
  "gender": "Female",
  "address": "456 Oak Ave",
  "mobileNumber": "9876543220",
  "alternateNumber": "9876543221",
  "class": "10B",
  "idProof": "Passport",
  "shiftType": "Afternoon",
  "seatNumber": 2,
  "studentStatus": "Active"
}
```

### 8. Get Single Student by ID
```http
GET http://localhost:5000/api/student/1
Authorization: Bearer TOKEN

```

### 9. Update Student (Admin Only)
```http
PUT http://localhost:5000/api/student/1
Authorization: Bearer TOKEN
Content-Type: application/json

{
  "id": 1,
  "vdlId": "VDL001",
  "name": "John Doe Updated",
  "email": "john.updated@example.com",
  "fatherName": "James Doe",
  "dateOfBirth": "2005-05-15T00:00:00",
  "gender": "Male",
  "address": "123 Main St Updated",
  "mobileNumber": "9876543210",
  "alternateNumber": "9876543211",
  "class": "10A",
  "idProof": "Aadhar",
  "shiftType": "Morning",
  "seatNumber": 1,
  "studentStatus": "Active",
  "createdDate": "2025-02-01T10:00:00",
  "updatedDate": "2025-02-09T15:30:00"
}
```

### 10. Delete Student (Admin Only)
```http
DELETE http://localhost:5000/api/student/1
Authorization: Bearer TOKEN

```

**Response (200):**
```json
{
  "message": "Student with ID 1 has been successfully deleted."
}
```

## Testing With cURL

### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "AdminPassword123!@#"
  }'
```

### Get Students (with token)
```bash
TOKEN="your_jwt_token_here"
curl -X GET http://localhost:5000/api/student \
  -H "Authorization: Bearer $TOKEN"
```

### Register New User
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "newuser",
    "email": "newuser@vdlcrm.com",
    "password": "NewPass123!@#",
    "roleId": 2
  }'
```

## Role-Based Endpoint Summary

| Endpoint | Method | Public | Auth Required | Admin | Internal | External | Student |
|----------|--------|--------|---------------|-------|----------|----------|---------|
| `/api/auth/login` | POST | ✅ | ❌ | - | - | - | - |
| `/api/auth/register` | POST | ✅ | ❌ | - | - | - | - |
| `/api/auth/roles` | GET | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `/api/student/register` | POST | ✅ | ❌ | - | - | - | - |
| `/api/student` | GET | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| `/api/student/{id}` | GET | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `/api/student/{id}` | PUT | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| `/api/student/{id}` | DELETE | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |

**Legend:**
- ✅ = Allowed
- ❌ = Not Allowed
- `-` = Not Applicable
