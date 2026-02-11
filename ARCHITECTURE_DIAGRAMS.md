# System Architecture & Flow Diagrams

## Authentication Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     CLIENT APPLICATION                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                    1. Register / Login
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │      AUTH CONTROLLER                    │
        │  /api/auth/register (Public)            │
        │  /api/auth/login (Public)               │
        │  /api/auth/roles (Protected)            │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │      AUTH SERVICE                       │
        │  • Hash/Verify Password (BCrypt)        │
        │  • Generate JWT Token                   │
        │  • Validate Credentials                 │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │      APP DB CONTEXT                     │
        │  • Users DbSet                          │
        │  • Roles DbSet                          │
        │  • Student DbSet                        │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │      SQLITE DATABASE                    │
        │  ┌───────────────────────────────────┐  │
        │  │ roles table                       │  │
        │  │ - RoleSequenceId (PK)             │  │
        │  │ - RoleName                        │  │
        │  │ - RoleId (Unique)                 │  │
        │  └───────────────────────────────────┘  │
        │  ┌───────────────────────────────────┐  │
        │  │ users table                       │  │
        │  │ - Id (PK)                         │  │
        │  │ - Username                        │  │
        │  │ - Email                           │  │
        │  │ - PasswordHash (BCrypt)           │  │
        │  │ - RoleId (FK)                     │  │
        │  │ - IsActive                        │  │
        │  │ - CreatedDate, UpdatedDate        │  │
        │  └───────────────────────────────────┘  │
        │  ┌───────────────────────────────────┐  │
        │  │ student_details table             │  │
        │  │ - Id (PK)                         │  │
        │  │ - VdlId, Name, Email              │  │
        │  │ - ... other fields                │  │
        │  └───────────────────────────────────┘  │
        └─────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │   2. JWT Token Returned                 │
        │   {success, message, token, user}       │
        └─────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CLIENT STORES TOKEN                          │
│              (localStorage, sessionStorage, etc)                │
└─────────────────────────────────────────────────────────────────┘
```

## Protected Endpoint Access Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                  CLIENT REQUEST                                 │
│          POST /api/student with Authorization header            │
│      Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI...       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
        ┌─────────────────────────────────────────┐
        │  AUTHENTICATE MIDDLEWARE                │
        │  • Extract token from header            │
        │  • Validate token signature             │
        │  • Check token expiration               │
        └─────────────────────────────────────────┘
                              │
                    ┌─────────┴─────────┐
                    │                   │
              Invalid Token        Valid Token
                    │                   │
                    ▼                   ▼
            ┌──────────────┐   ┌──────────────────────┐
            │ Return 401   │   │ Extract Claims       │
            │ Unauthorized │   │ • UserId             │
            └──────────────┘   │ • Username           │
                                │ • RoleId             │
                                │ • RoleName           │
                                └──────────────────────┘
                                          │
                                          ▼
                        ┌───────────────────────────────┐
                        │  AUTHORIZE MIDDLEWARE          │
                        │  Check Role Requirements       │
                        │  /api/student GET:             │
                        │    Requires: Admin, Internal   │
                        └───────────────────────────────┘
                                          │
                        ┌─────────────────┴──────────────┐
                        │                                │
                   Role OK                         Role Not OK
                        │                                │
                        ▼                                ▼
        ┌───────────────────────────┐   ┌──────────────────────┐
        │  CONTROLLER ACTION         │   │  Return 403          │
        │  StudentController.        │   │  Forbidden           │
        │  GetAllStudents()          │   │                      │
        └───────────────────────────┘   └──────────────────────┘
                        │
                        ▼
        ┌───────────────────────────┐
        │  BUSINESS LOGIC            │
        │  StudentService.           │
        │  GetAllStudentsAsync()     │
        └───────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────┐
        │  DATABASE QUERY            │
        │  Get all students          │
        └───────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────┐
        │  Return 200 OK             │
        │  [{ Student Objects }]     │
        └───────────────────────────┘
```

## Role Hierarchy

```
┌─────────────────────────────────────────────────────────────────┐
│                        ROLES HIERARCHY                          │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│ Admin (RoleId: 1)                                                │
│ ├─ ✅ View all students                                          │
│ ├─ ✅ Create students                                            │
│ ├─ ✅ Update students                                            │
│ ├─ ✅ Delete students                                            │
│ ├─ ✅ Manage users                                               │
│ └─ ✅ Full system access                                         │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│ Internal User (RoleId: 2)                                        │
│ ├─ ✅ View all students                                          │
│ ├─ ✅ Create students                                            │
│ ├─ ❌ Update students                                            │
│ ├─ ❌ Delete students                                            │
│ └─ ❌ Manage users                                               │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│ External User (RoleId: 3)                                        │
│ ├─ ❌ View all students (List)                                   │
│ ├─ ✅ View specific student (by ID)                              │
│ ├─ ❌ Create students                                            │
│ ├─ ❌ Update students                                            │
│ ├─ ❌ Delete students                                            │
│ └─ ❌ Manage users                                               │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│ Student (RoleId: 4)                                              │
│ ├─ ❌ View all students                                          │
│ ├─ ✅ View own student record                                    │
│ ├─ ❌ Create/Update/Delete                                       │
│ └─ ❌ Manage users                                               │
└──────────────────────────────────────────────────────────────────┘
```

## JWT Token Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                    JWT TOKEN STRUCTURE                          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ HEADER                                                          │
│ {                                                               │
│   "alg": "HS256",         // Algorithm                          │
│   "typ": "JWT"            // Type                               │
│ }                                                               │
└─────────────────────────────────────────────────────────────────┘
                              ·
┌─────────────────────────────────────────────────────────────────┐
│ PAYLOAD (Claims)                                                │
│ {                                                               │
│   "sub": "1",                    // User ID                     │
│   "unique_name": "admin",        // Username                    │
│   "email": "admin@vdlcrm.com",   // Email                       │
│   "RoleId": "1",                 // Role ID                     │
│   "RoleName": "Admin",           // Role Name                   │
│   "role": "Admin",               // Standard Role Claim         │
│   "iss": "VdlcrmApi",            // Issuer                      │
│   "aud": "VdlcrmUsers",          // Audience                    │
│   "exp": 1739107264,             // Expiration Time             │
│   "iat": 1739103664              // Issued At                   │
│ }                                                               │
└─────────────────────────────────────────────────────────────────┘
                              ·
┌─────────────────────────────────────────────────────────────────┐
│ SIGNATURE                                                       │
│ HMACSHA256(                                                     │
│   base64UrlEncode(header) + "." +                               │
│   base64UrlEncode(payload),                                     │
│   "your-super-secret-key-that-is-long-enough"                  │
│ )                                                               │
└─────────────────────────────────────────────────────────────────┘

Result Token Format:
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
eyJzdWIiOiIxIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AZXhhbXBsZS5jb20iLCJSb2xlSWQiOiIxIiwiUm9sZU5hbWUiOiJBZG1pbiIsInJvbGUiOiJBZG1pbiIsImlzcyI6IlZkbGNybUFwaSIsImF1ZCI6IlZkbGNybVVzZXJzIiwiZXhwIjoxNzM5MTA3MjY0LCJpYXQiOjE3MzkxMDM2NjR9.
signature_here...
```

## Endpoint Access Matrix

```
┌──────────────────────────────────────────────────────────────────┐
│                    ENDPOINT ACCESS CONTROL                       │
└──────────────────────────────────────────────────────────────────┘

                     │ None │ Admin │ Internal │ External │ Student │
                     │      │ (1)   │ User (2) │ User (3) │ (4)     │
─────────────────────┼──────┼───────┼──────────┼──────────┼─────────┤
POST /auth/register  │  ✅   │  ✅   │    ✅    │    ✅    │   ✅    │
POST /auth/login     │  ✅   │  ✅   │    ✅    │    ✅    │   ✅    │
GET  /auth/roles     │  ❌   │  ✅   │    ✅    │    ✅    │   ✅    │
─────────────────────┼──────┼───────┼──────────┼──────────┼─────────┤
POST /student/reg    │  ✅   │  ✅   │    ✅    │    ✅    │   ✅    │
GET  /student        │  ❌   │  ✅   │    ✅    │    ❌    │   ❌    │
GET  /student/{id}   │  ❌   │  ✅   │    ✅    │    ✅    │   ✅    │
PUT  /student/{id}   │  ❌   │  ✅   │    ❌    │    ❌    │   ❌    │
DEL  /student/{id}   │  ❌   │  ✅   │    ❌    │    ❌    │   ❌    │
─────────────────────┴──────┴───────┴──────────┴──────────┴─────────┘

Legend:
- None    = No authentication required (Public)
- Admin   = Admin role required
- Internal User = Admin or Internal User role required
- External User = Any authenticated user can access
- Student = Specific roles as shown
```

## Database Relationships

```
┌──────────────────────────────────────────────────────────────────┐
│                    ENTITY RELATIONSHIPS                          │
└──────────────────────────────────────────────────────────────────┘

                        ┌──────────────┐
                        │    Role      │
                        ├──────────────┤
                        │ RoleSequenceId│ ◄────────────┐
                        │ RoleName     │              │
                        │ RoleId (PK)  │              │
                        └──────────────┘              │
                              ▲                       │
                              │                       │
                              │ (1:N) OneToMany      │
                              │ RoleId → RoleId      │
                              │                       │
                        ┌──────────────┐              │
                        │   User       │              │
                        ├──────────────┤              │
                        │ Id           │              │
                        │ Username     │              │
                        │ Email        │              │
                        │ PasswordHash │              │
                        │ RoleId (FK)  │──────────────┘
                        │ IsActive     │
                        │ CreatedDate  │
                        │ UpdatedDate  │
                        └──────────────┘


                        ┌──────────────────┐
                        │ Student          │
                        ├──────────────────┤
                        │ Id               │
                        │ VdlId            │
                        │ Name             │
                        │ Email            │
                        │ FatherName       │
                        │ DateOfBirth      │
                        │ Gender           │
                        │ Address          │
                        │ MobileNumber     │
                        │ AlternateNumber  │
                        │ Class            │
                        │ IdProof          │
                        │ ShiftType        │
                        │ SeatNumber       │
                        │ StudentStatus    │
                        │ CreatedDate      │
                        │ UpdatedDate      │
                        └──────────────────┘

Note: Student table is independent and can be linked
to User table via email during application logic.
```

## Request/Response Cycle

```
┌─────────────────────────────────────────────────────────────────┐
│                    REQUEST → RESPONSE CYCLE                     │
└─────────────────────────────────────────────────────────────────┘

START
  │
  ▼
┌─────────────────────────────────────────────────────────────────┐
│ 1. CLIENT REQUEST                                               │
│    Method: GET/POST/PUT/DELETE                                  │
│    Endpoint: /api/student                                       │
│    Headers: Authorization: Bearer <JWT_TOKEN>                   │
│    Body: JSON (if applicable)                                   │
└─────────────────────────────────────────────────────────────────┘
  │
  ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. AUTHENTICATION MIDDLEWARE                                    │
│    ✓ Extract token from header                                  │
│    ✓ Validate signature using secret key                        │
│    ✓ Check expiration                                           │
│    ✓ Build ClaimsPrincipal from token                           │
└─────────────────────────────────────────────────────────────────┘
  │
  ├─ Invalid/Expired Token ─────────────────┐
  │                                          ▼
  │                              Return 401 Unauthorized
  │
  ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. AUTHORIZATION MIDDLEWARE                                     │
│    ✓ Check [Authorize] attribute                                │
│    ✓ Check role requirements                                    │
│    ✓ Verify user has required role                              │
└─────────────────────────────────────────────────────────────────┘
  │
  ├─ Insufficient Permissions ──────────────┐
  │                                          ▼
  │                              Return 403 Forbidden
  │
  ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. CONTROLLER ACTION                                            │
│    StudentController.GetAllStudents()                           │
│    ✓ Validate request data                                      │
│    ✓ Process business logic                                     │
└─────────────────────────────────────────────────────────────────┘
  │
  ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. SERVICE LAYER                                                │
│    StudentService.GetAllStudentsAsync()                         │
│    ✓ Apply business rules                                       │
│    ✓ Interact with database                                     │
└─────────────────────────────────────────────────────────────────┘
  │
  ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. DATABASE LAYER                                               │
│    LINQ query to SQLite                                         │
│    ✓ Fetch data based on criteria                               │
└─────────────────────────────────────────────────────────────────┘
  │
  ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. RESPONSE                                                     │
│    Status: 200 OK                                               │
│    Headers: Content-Type: application/json                      │
│    Body: JSON response with data                                │
└─────────────────────────────────────────────────────────────────┘
  │
  ▼
END
```
