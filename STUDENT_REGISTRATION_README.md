# Student Registration Module

## Overview
This document describes the Student Registration functionality added to the VDLCRM system. The module handles the registration and management of students with a complete CRUD (Create, Read, Update, Delete) API.

## Changes Made

### 1. Data Model
**File:** [Vdlcrm.Model/Student.cs](Vdlcrm.Model/Student.cs)

Created a `Student` model class with the following properties (all converted to camelCase):
- `Id` - Primary key (integer)
- `VdlId` - Student VDL ID (string)
- `Name` - Full name of the student (string)
- `Email` - Email address (string)
- `FatherName` - Father's name (string)
- `DateOfBirth` - Date of birth (DateTime) - *Fixed typo from "dob"*
- `Gender` - Gender (string)
- `Address` - Residential address (string)
- `MobileNumber` - Primary mobile number (string) - *Fixed typo from "Mobile_num"*
- `AlternateNumber` - Alternate contact number (string) - *Fixed typo from "Alternate_num"*
- `Class` - Class/Grade (string)
- `IdProof` - ID proof document name (string) - *Fixed typo from "Id_proof"*
- `ShiftType` - Shift type (Morning/Afternoon/Evening) (string) - *Fixed typo from "Shift_type"*
- `SeatNumber` - Seat assignment number (integer) - *Fixed typo from "Seat_num"*
- `StudentStatus` - Status of the student (string) - *Fixed camelCase from "Student_Status"*
- `CreatedDate` - Record creation timestamp (DateTime) - *Fixed typo from "Created_Date"*
- `UpdatedDate` - Last update timestamp (DateTime) - *Fixed typo from "Updated_date"*

### 2. Database Configuration
**File:** [Vdlcrm.Services/AppDbContext.cs](Vdlcrm.Services/AppDbContext.cs)

- Added `DbSet<Student> StudentDetails` to the DbContext
- Configured the Student entity to use table name `student_details`
- Added data annotations and constraints for all properties
- Set required fields and max lengths for string properties

### 3. Service Layer
**File:** [Vdlcrm.Services/StudentService.cs](Vdlcrm.Services/StudentService.cs)

Implemented `StudentService` class with the following methods:

#### `RegisterStudentAsync(Student studentDetails)` - Main Registration Method
- Validates all student details
- Sets `CreatedDate` and `UpdatedDate` to current UTC time
- Sets default `StudentStatus` to "Active" if not provided
- Adds the student to the repository
- **Validation Rules:**
  - All required fields must be non-empty
  - Email must be in valid format
  - Date of birth must be in the past
  - Mobile numbers must be valid phone format (10 digits or international format)
  - Seat number must be greater than 0

#### Other Methods:
- `GetAllStudentsAsync()` - Retrieves all registered students
- `GetStudentByIdAsync(int id)` - Retrieves a specific student by ID
- `UpdateStudentAsync(Student student)` - Updates existing student details
- `DeleteStudentAsync(int id)` - Deletes a student record

### 4. Controller
**File:** [Vdlcrm.Web/Controllers/Account/StudentController.cs](Vdlcrm.Web/Controllers/Account/StudentController.cs)

Implemented REST API endpoints:

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/student/register` | Register a new student |
| GET | `/api/student` | Get all students |
| GET | `/api/student/{id}` | Get student by ID |
| PUT | `/api/student/{id}` | Update student details |
| DELETE | `/api/student/{id}` | Delete a student |

**Response Codes:**
- `201 Created` - Successful student registration
- `200 OK` - Successful GET or UPDATE operation
- `400 Bad Request` - Validation errors
- `404 Not Found` - Student not found
- `500 Internal Server Error` - Server errors

### 5. Dependency Injection
**File:** [Vdlcrm.Web/Program.cs](Vdlcrm.Web/Program.cs)

- Registered `StudentService` in the DI container using `builder.Services.AddScoped<StudentService>()`

### 6. Project References
**File:** [Vdlcrm.Services/Vdlcrm.Services.csproj](Vdlcrm.Services/Vdlcrm.Services.csproj)

- Added project reference to `Vdlcrm.Utilities` for accessing the `DateTimeHelper` utility class

### 7. Database Migration
**File:** [Vdlcrm.Services/Migrations/](Vdlcrm.Services/Migrations/)

Created EF Core migration to create the `student_details` table in the SQLite database with all necessary columns and constraints.

## API Usage Examples

### Register a Student
```http
POST /api/student/register HTTP/1.1
Content-Type: application/json

{
  "vdlId": "VDL001",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "fatherName": "James Doe",
  "dateOfBirth": "2008-05-15",
  "gender": "Male",
  "address": "123 Main Street, City, State 12345",
  "mobileNumber": "9876543210",
  "alternateNumber": "9876543211",
  "class": "10th",
  "idProof": "Aadhar123456789",
  "shiftType": "Morning",
  "seatNumber": 1,
  "studentStatus": "Active"
}
```

### Response
```json
{
  "id": 1,
  "vdlId": "VDL001",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "fatherName": "James Doe",
  "dateOfBirth": "2008-05-15T00:00:00",
  "gender": "Male",
  "address": "123 Main Street, City, State 12345",
  "mobileNumber": "9876543210",
  "alternateNumber": "9876543211",
  "class": "10th",
  "idProof": "Aadhar123456789",
  "shiftType": "Morning",
  "seatNumber": 1,
  "studentStatus": "Active",
  "createdDate": "2026-02-04T09:50:00Z",
  "updatedDate": "2026-02-04T09:50:00Z"
}
```

## Testing the API

Use the provided `Student.http` file in VS Code with the REST Client extension to test all endpoints:

```
File: Vdlcrm.Web/Student.http
```

## Typo Fixes Applied

1. **dob** → `DateOfBirth` - More descriptive and follows camelCase convention
2. **Mobile_num** → `MobileNumber` - camelCase conversion
3. **Alternate_num** → `AlternateNumber` - camelCase conversion
4. **Id_proof** → `IdProof` - camelCase conversion
5. **Shift_type** → `ShiftType` - camelCase conversion
6. **Seat_num** → `SeatNumber` - camelCase conversion
7. **Student_Status** → `StudentStatus` - camelCase conversion
8. **Created_Date** → `CreatedDate` - camelCase conversion
9. **Updated_date** → `UpdatedDate` - camelCase conversion (fixed inconsistent casing)

## Validation Features

The registration module includes comprehensive validation:

- **Email Validation:** Uses .NET's MailAddress to validate email format
- **Phone Number Validation:** Validates 10-digit or international phone formats
- **Date Validation:** Ensures date of birth is in the past
- **Required Fields:** All required fields are validated before storage
- **Boundary Validation:** Seat number must be greater than 0

## Database Table Schema

**Table Name:** `student_details`

```sql
CREATE TABLE "student_details" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "VdlId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "FatherName" TEXT NOT NULL,
    "DateOfBirth" TEXT NOT NULL,
    "Gender" TEXT NOT NULL,
    "Address" TEXT NOT NULL,
    "MobileNumber" TEXT NOT NULL,
    "AlternateNumber" TEXT NOT NULL,
    "Class" TEXT NOT NULL,
    "IdProof" TEXT NOT NULL,
    "ShiftType" TEXT NOT NULL,
    "SeatNumber" INTEGER NOT NULL,
    "StudentStatus" TEXT NOT NULL,
    "CreatedDate" TEXT NOT NULL,
    "UpdatedDate" TEXT NOT NULL
);
```

## Next Steps

1. Test the API endpoints using the provided `Student.http` file
2. Run the application: `dotnet run`
3. Access the API at `http://localhost:5000/api/student`
4. Integrate with frontend application
5. Add additional business logic as required
