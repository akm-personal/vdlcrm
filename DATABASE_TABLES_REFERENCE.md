# 📊 Database Tables Reference

## All Database Tables in VDLCRM

### **Total Tables: 4**

---

## 1️⃣ **ROLES Table** (Reference Data)

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| **RoleSequenceId** | INT | PK, AutoIncrement | Auto-generated primary key |
| **RoleName** | VARCHAR(100) | NOT NULL, UNIQUE | Role name (Admin, Internal User, etc) |
| **RoleId** | INT | NOT NULL, UNIQUE | Identifier for role lookup (1, 2, 3, 4) |

**Pre-seeded Data:**
```
RoleSequenceId | RoleName         | RoleId
1              | Admin            | 1
2              | Internal User    | 2
3              | External User    | 3
4              | Student          | 4
```

**Used By:** User registration & login to assign roles

---

## 2️⃣ **USERS Table** (🔑 **LOGIN USES THIS TABLE**)

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| **Id** | INT | PK, AutoIncrement | User unique identifier |
| **Username** | VARCHAR(100) | NOT NULL, UNIQUE | Login username |
| **Email** | VARCHAR(100) | NOT NULL, UNIQUE | User email address |
| **PasswordHash** | TEXT | NOT NULL | BCrypt hashed password |
| **RoleId** | INT | NOT NULL, FK → roles(RoleId) | User's role (1-4) |
| **IsActive** | BOOLEAN | NOT NULL | Active/Not Active user flag |
| **CreatedDate** | DATETIME | NOT NULL | Account creation timestamp |
| **UpdatedDate** | DATETIME | NOT NULL | Last profile update timestamp |

**Index:** Username (for fast login lookup)

**Sample Data:**
```
Id | Username | Email              | PasswordHash    | RoleId | IsActive | CreatedDate
1  | admin    | admin@example.com  | $2a$11$...     | 1      | 1        | 2026-02-09
2  | user2    | user2@example.com  | $2a$11$...     | 2      | 1        | 2026-02-09
3  | user3    | user3@example.com  | $2a$11$...     | 3      | 1        | 2026-02-09
4  | student  | student@example... | $2a$11$...     | 4      | 1        | 2026-02-09
```

**Relationships:**
- Foreign Key: `RoleId` → `roles.RoleId`

---

## 3️⃣ **STUDENT_DETAILS Table** (Student Information)

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| **Id** | INT | PK, AutoIncrement | Student unique identifier |
| **VdlId** | VARCHAR(50) | NOT NULL | VDL system ID |
| **Name** | VARCHAR(100) | NOT NULL | Student full name |
| **Email** | VARCHAR(100) | NOT NULL | Student email |
| **FatherName** | VARCHAR(100) | NOT NULL | Father's name |
| **DateOfBirth** | DATETIME | NOT NULL | DOB |
| **Gender** | VARCHAR(20) | NOT NULL | Male/Female |
| **Address** | VARCHAR(255) | NOT NULL | Residential address |
| **MobileNumber** | VARCHAR(20) | NOT NULL | Contact number |
| **AlternateNumber** | VARCHAR(20) | NULLABLE | Alternate contact |
| **Class** | VARCHAR(50) | NOT NULL | Student class (10A, etc) |
| **IdProof** | VARCHAR(100) | NOT NULL | ID proof type |
| **ShiftType** | VARCHAR(50) | NOT NULL | Morning/Afternoon |
| **SeatNumber** | INT | NOT NULL | Assigned seat number |
| **StudentStatus** | VARCHAR(50) | NOT NULL | Active/Not Active |
| **CreatedDate** | DATETIME | NOT NULL | Registration date |
| **UpdatedDate** | DATETIME | NOT NULL | Last update date |

**Note:** Not directly related to login but protected by JWT token

---

## 4️⃣ **WEATHERFORECASTS Table** (Legacy/Sample Data)

| Column | Type | Constraints | Purpose |
|--------|------|-------------|---------|
| **Id** | INT | PK, AutoIncrement | Forecast ID |
| **Date** | DATE | NOT NULL | Forecast date |
| **TemperatureC** | INT | NOT NULL | Temperature Celsius |
| **TemperatureF** | INT | COMPUTED | Temperature Fahrenheit |
| **Summary** | VARCHAR(50) | NULLABLE | Weather summary |

**Note:** Example/legacy table, not related to authentication

---

## 🔐 **LOGIN METHOD - WHICH TABLE?**

### The login method uses the **USERS** table

**Login Flow:**

```
POST /api/auth/login
{
  "username": "admin",
  "password": "AdminPassword123!@#"
}
    ↓
AuthService.LoginAsync()
    ↓
Query USERS table:
  SELECT * FROM users 
  WHERE Username = 'admin'
    ↓
Check:
  1. User exists?
  2. User is active? (IsActive = true)
  3. Password matches? (BCrypt.Verify)
  4. Get RoleId from user record
    ↓
Query ROLES table:
  SELECT * FROM roles 
  WHERE RoleId = user.RoleId
    ↓
Generate JWT Token with:
  - UserId
  - Username
  - Email
  - RoleId
  - RoleName (from roles table)
    ↓
Return Token + User Info
```

### **Exact Code from AuthService.cs:**

```csharp
// Find user in USERS table
var user = await _context.Users
    .Include(u => u.Role)  // Join with ROLES table
    .FirstOrDefaultAsync(u => u.Username == request.Username);

// Check if user exists and is active
if (user == null || !user.IsActive)
{
    return new LoginResponse { Success = false, ... };
}

// Verify password hash from USERS table
if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
{
    return new LoginResponse { Success = false, ... };
}

// Get role name from ROLES table (already loaded via Include)
var token = GenerateJwtToken(user);

return new LoginResponse
{
    Success = true,
    User = new UserDto
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        RoleId = user.RoleId,
        RoleName = user.Role?.RoleName ?? "Unknown",  // From ROLES table
        IsActive = user.IsActive
    }
};
```

---

## 📍 **Which Tables are Used Where**

### USERS Table (Primary Login Table)
✅ **Login** - Query by username + verify password  
✅ **Register** - Insert new user record  
✅ **Verify Active** - Check IsActive flag  
✅ **Get User Info** - Fetch user details  

### ROLES Table (Supporting Login)
✅ **Join on Login** - Get user's role name  
✅ **Role Validation** - Verify role exists  
✅ **Role Display** - Show role name in response  
✅ **Seed Initial Roles** - 4 predefined roles  

### STUDENT_DETAILS Table
❌ **Not used directly in login**  
✅ **Used in student endpoints** - Protected by JWT token from USERS table  
✅ **Can link to USERS** - Via email (application logic)  

### WEATHERFORECASTS Table
❌ **Not used in login**  
✅ **Legacy/sample data** - For demonstration  

---

## 🔄 **Login Query Sequence**

```
Step 1: Extract username from request
Step 2: SELECT * FROM users WHERE username = ?
Step 3: Check if record exists
Step 4: Verify BCrypt(password, user.passwordHash)
Step 5: Check user.IsActive = 1
Step 6: SELECT * FROM roles WHERE roleId = user.roleId
Step 7: Build JWT token with user + role data
Step 8: Return response with token
```

---

## 📊 **Table Relationships**

```
ROLES (1)
   ↓
   1:N (One Role has Many Users)
   ↓
USERS (N)
   ↑
   └─→ RoleId (Foreign Key to ROLES.RoleId)

USERS (N)
   ↓
   Can be linked to STUDENT_DETAILS (1:1 via Email)
   ↓
STUDENT_DETAILS (1)
```

---

## 🛠️ **SQL Queries for Login**

### Query 1: Find User
```sql
SELECT 
    u.Id,
    u.Username,
    u.Email,
    u.PasswordHash,
    u.RoleId,
    u.IsActive,
    r.RoleName
FROM users u
LEFT JOIN roles r ON u.RoleId = r.RoleId
WHERE u.Username = 'admin';
```

### Query 2: Verify Login (Pseudocode)
```sql
-- Check if user exists and is active
SELECT COUNT(*) FROM users 
WHERE Username = 'admin' AND IsActive = 1;

-- Get user details
SELECT Id, Username, Email, PasswordHash, RoleId 
FROM users 
WHERE Username = 'admin';

-- Get role name
SELECT RoleName FROM roles WHERE RoleId = user.RoleId;
```

---

## 📋 **Database Statistics**

| Table | Purpose | Rows (Initial) | Critical |
|-------|---------|---|----------|
| **roles** | Role definitions | 4 | ✅ YES (reference) |
| **users** | User accounts | Varies | ✅ YES (login) |
| **student_details** | Student records | Varies | ⚠️ NO (optional) |
| **WeatherForecasts** | Sample data | N/A | ❌ NO (legacy) |

---

## ✅ **Login Summary**

**Primary Table:** `users`  
**Supporting Table:** `roles`  
**Key Fields Used:**
- `users.Username` - For lookup
- `users.PasswordHash` - For verification
- `users.IsActive` - For status check
- `users.RoleId` - To join with roles
- `roles.RoleName` - For token claims

**No other tables are queried during login.**

---

## 🔍 **How to View Database**

### Using SQLite CLI
```bash
sqlite3 vdlcrm.db

# List all tables
.tables

# View USERS table
SELECT * FROM users;

# View ROLES table
SELECT * FROM roles;

# View schema
.schema users
.schema roles
```

### Using VS Code Extension
1. Install "SQLite" extension
2. Open `/workspaces/vdlcrm/vdlcrm.db`
3. Browse tables in sidebar
4. Execute SQL queries

---

**Last Updated:** February 9, 2026  
**Status:** ✅ Complete
