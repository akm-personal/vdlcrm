# Database Migration Guide

## Overview
This guide explains how to apply the role-based authentication database migration to your VDLCRM database.

## Prerequisites
- .NET 10.0 SDK installed
- SQLite installed (or already configured)
- Project builds successfully (`dotnet build`)

## Step-by-Step Migration

### 1. Navigate to the Web Project
```bash
cd /workspaces/vdlcrm/Vdlcrm.Web
```

### 2. Apply Migration
```bash
dotnet ef database update --project ../Vdlcrm.Services/Vdlcrm.Services.csproj
```

**Output (expected):**
```
info: Microsoft.EntityFrameworkCore.Infrastructure[10403]
      Entity Framework Core 10.0.2 initialized 'AppDbContext' using provider 'Microsoft.EntityFrameworkCore.Sqlite' with options: none
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Migrating database to migration '20260209120000_AddRoleAndUserTables'.
Done.
```

### 3. Verify Migration
The following tables should be created:
- `roles` - Contains role definitions
- `users` - Contains user accounts

### 4. Check Database
```bash
# Using sqlite3 command-line tool
sqlite3 vdlcrm.db

# Inside sqlite3:
.tables  # Should show: roles, users, student_details, WeatherForecasts
.schema roles
.schema users

# View seeded roles
SELECT * FROM roles;
```

**Expected roles output:**
```
1|Admin|1
2|Internal User|2
3|External User|3
4|Student|4
```

## If Migration Fails

### Issue: "Cannot find column RoleId"
**Cause:** Previous version of roles table exists
**Solution:**
```bash
# Remove the database file and recreate
rm vdlcrm.db
dotnet ef database update --project ../Vdlcrm.Services/Vdlcrm.Services.csproj
```

### Issue: "Sqlite database is locked"
**Cause:** Another process has the database open
**Solution:**
```bash
# Kill any processes using the database and try again
# Or close any running API instances
dotnet ef database update --project ../Vdlcrm.Services/Vdlcrm.Services.csproj
```

### Issue: "Build errors"
**Cause:** Dependencies not restored
**Solution:**
```bash
cd /workspaces/vdlcrm
dotnet restore
dotnet build
```

## Rollback (If Needed)

To rollback to the previous migration:
```bash
# Remove the latest migration
dotnet ef migrations remove --project ../Vdlcrm.Services/Vdlcrm.Services.csproj

# Update database to previous state
dotnet ef database update --project ../Vdlcrm.Services/Vdlcrm.Services.csproj
```

**Warning:** This will delete the roles and users tables.

## Testing the Migration

### 1. Start the API
```bash
dotnet run
```

### 2. Register an Admin User
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "email": "admin@vdlcrm.com",
    "password": "AdminPassword123!@#",
    "roleId": 1
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "User registered successfully"
}
```

### 3. Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "AdminPassword123!@#"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
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

### 4. Test Protected Endpoint
```bash
TOKEN="<copy_token_from_login_response>"

curl -X GET http://localhost:5000/api/student \
  -H "Authorization: Bearer $TOKEN"
```

**Expected:** Student list or empty array (depending on existing data)

## Database File Location

SQLite database file location:
- Development: `./vdlcrm.db` (in the working directory)
- Default connection string: `Data Source=vdlcrm.db`

## Database Schema

### Roles Table Structure
```sql
-- RoleSequenceId: Auto-increment primary key
-- RoleName: String (100 chars max)
-- RoleId: Integer unique identifier (1-4)
CREATE TABLE roles (
    RoleSequenceId INTEGER PRIMARY KEY AUTOINCREMENT,
    RoleName TEXT NOT NULL UNIQUE,
    RoleId INTEGER NOT NULL UNIQUE
);
```

### Users Table Structure
```sql
-- Id: Auto-increment primary key
-- Username: Unique username (100 chars max)
-- Email: Unique email (100 chars max)
-- PasswordHash: BCrypt hashed password
-- RoleId: Foreign key to roles table
-- IsActive: Boolean flag
-- Timestamps: UTC datetime
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

## Viewing Database Contents

### Using SQLite CLI
```bash
# Open database
sqlite3 vdlcrm.db

# View all tables
.tables

# View roles
SELECT * FROM roles;

# View users
SELECT id, username, email, roleId FROM users;

# Exit
.quit
```

### Using VS Code SQLite Extension
1. Install "SQLite" extension
2. Open Command Palette (Ctrl+Shift+P)
3. Select "SQLite: Open Database"
4. Select `vdlcrm.db`
5. Explore tables in the sidebar

## Troubleshooting

### Tables Not Found After Migration
```bash
# Check if database exists
ls -la vdlcrm.db

# If it doesn't exist, run migration again
dotnet ef database update --project ../Vdlcrm.Services/Vdlcrm.Services.csproj

# If it exists but tables are missing, check entity configuration
# Verify AppDbContext.cs has Role and User DbSets
```

### Old Database Issues
If you have an old database from before this migration:
```bash
# Backup old database
cp vdlcrm.db vdlcrm.db.backup

# Delete old database
rm vdlcrm.db

# Run migration to create new database with roles/users tables
dotnet ef database update --project ../Vdlcrm.Services/Vdlcrm.Services.csproj
```

## Need More Help?

See detailed documentation:
- `AUTH_API_DOCUMENTATION.md` - API endpoints and JWT tokens
- `API_TEST_EXAMPLES.md` - Request/response examples
- `AUTHENTICATION_IMPLEMENTATION.md` - Implementation details
