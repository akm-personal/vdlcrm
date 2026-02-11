using Microsoft.Extensions.Logging;
using Vdlcrm.Interfaces;
using Vdlcrm.Model;
using Vdlcrm.Utilities;

namespace Vdlcrm.Services;

public class StudentService
{
    private readonly IRepository<Student> _studentRepository;
    private readonly RegistrationService _registrationService;
    private readonly ILogger<StudentService> _logger;

    public StudentService(IRepository<Student> studentRepository, RegistrationService registrationService, ILogger<StudentService> logger)
    {
        _studentRepository = studentRepository;
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new student with provided details - Creates user account automatically
    /// Generates auto VDL ID (VDL001, VDL002, etc.) and temporary password
    /// Thread-safe for concurrent/bulk registrations
    /// </summary>
    /// <param name="studentDetails">Student registration details (VdlId can be empty, will be auto-generated)</param>
    /// <returns>Tuple with student, user, and temporary password</returns>
    public async Task<(Student student, User user, string tempPassword)> RegisterStudentAsync(Student studentDetails)
    {
        if (studentDetails == null)
        {
            throw new ArgumentNullException(nameof(studentDetails), "Student details cannot be null.");
        }

        // Validate student details (skip VdlId validation as it will be auto-generated)
        ValidateStudentDetailsForRegistration(studentDetails);

        try
        {
            // Use RegistrationService to handle concurrent registration with transactions
            var (student, user, tempPassword) = await _registrationService.RegisterStudentWithUserAsync(studentDetails);
            
            _logger.LogInformation($"Student registered successfully: VdlId={student.VdlId}, Username={user.Username}");
            
            return (student, user, tempPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error registering student: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Register student (legacy method - for backward compatibility)
    /// </summary>
    /// <param name="studentDetails">Student registration details</param>
    /// <returns>Registered student object</returns>
    public async Task<Student> RegisterStudentLegacyAsync(Student studentDetails)
    {
        if (studentDetails == null)
        {
            throw new ArgumentNullException(nameof(studentDetails), "Student details cannot be null.");
        }

        ValidateStudentDetails(studentDetails);

        // Set audit fields
        studentDetails.CreatedDate = DateTimeHelper.GetUtcNow();
        studentDetails.UpdatedDate = DateTimeHelper.GetUtcNow();

        // Default status for new registration
        if (string.IsNullOrEmpty(studentDetails.StudentStatus))
        {
            studentDetails.StudentStatus = "Active";
        }

        // Add student to repository
        var registeredStudent = await _studentRepository.AddAsync(studentDetails);

        return registeredStudent;
    }

    /// <summary>
    /// Get all registered students
    /// </summary>
    /// <returns>List of all students</returns>
    public async Task<IEnumerable<Student>> GetAllStudentsAsync()
    {
        return await _studentRepository.GetAllAsync();
    }

    /// <summary>
    /// Get a student by ID
    /// </summary>
    /// <param name="id">Student ID</param>
    /// <returns>Student object if found, null otherwise</returns>
    public async Task<Student?> GetStudentByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Student ID must be greater than 0.", nameof(id));
        }

        return await _studentRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Update an existing student's details
    /// </summary>
    /// <param name="student">Student object with updated details</param>
    /// <returns>Updated student object</returns>
    public async Task<Student> UpdateStudentAsync(Student student)
    {
        if (student == null)
        {
            throw new ArgumentNullException(nameof(student), "Student details cannot be null.");
        }

        if (student.Id <= 0)
        {
            throw new ArgumentException("Student ID must be greater than 0.", nameof(student.Id));
        }

        ValidateStudentDetails(student);

        // Update the UpdatedDate field
        student.UpdatedDate = DateTimeHelper.GetUtcNow();

        return await _studentRepository.UpdateAsync(student);
    }

    /// <summary>
    /// Delete a student by ID
    /// </summary>
    /// <param name="id">Student ID to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    public async Task<bool> DeleteStudentAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Student ID must be greater than 0.", nameof(id));
        }

        return await _studentRepository.DeleteAsync(id);
    }

    /// <summary>
    /// Validate student details
    /// </summary>
    /// <param name="student">Student to validate</param>
    private void ValidateStudentDetails(Student student)
    {
        if (string.IsNullOrWhiteSpace(student.VdlId))
            throw new ArgumentException("VDL ID is required.", nameof(student.VdlId));

        if (string.IsNullOrWhiteSpace(student.Name))
            throw new ArgumentException("Student name is required.", nameof(student.Name));

        if (string.IsNullOrWhiteSpace(student.Email))
            throw new ArgumentException("Email is required.", nameof(student.Email));

        if (!IsValidEmail(student.Email))
            throw new ArgumentException("Email format is invalid.", nameof(student.Email));

        if (string.IsNullOrWhiteSpace(student.FatherName))
            throw new ArgumentException("Father's name is required.", nameof(student.FatherName));

        if (student.DateOfBirth == default)
            throw new ArgumentException("Date of birth is required.", nameof(student.DateOfBirth));

        if (student.DateOfBirth >= DateTime.Now)
            throw new ArgumentException("Date of birth must be in the past.", nameof(student.DateOfBirth));

        if (string.IsNullOrWhiteSpace(student.Gender))
            throw new ArgumentException("Gender is required.", nameof(student.Gender));

        if (string.IsNullOrWhiteSpace(student.Address))
            throw new ArgumentException("Address is required.", nameof(student.Address));

        if (string.IsNullOrWhiteSpace(student.MobileNumber))
            throw new ArgumentException("Mobile number is required.", nameof(student.MobileNumber));

        if (!IsValidPhoneNumber(student.MobileNumber))
            throw new ArgumentException("Mobile number format is invalid.", nameof(student.MobileNumber));

        if (!string.IsNullOrWhiteSpace(student.AlternateNumber) && !IsValidPhoneNumber(student.AlternateNumber))
            throw new ArgumentException("Alternate number format is invalid.", nameof(student.AlternateNumber));

        if (string.IsNullOrWhiteSpace(student.Class))
            throw new ArgumentException("Class is required.", nameof(student.Class));

        if (string.IsNullOrWhiteSpace(student.IdProof))
            throw new ArgumentException("ID proof is required.", nameof(student.IdProof));

        if (string.IsNullOrWhiteSpace(student.ShiftType))
            throw new ArgumentException("Shift type is required.", nameof(student.ShiftType));

        if (student.SeatNumber <= 0)
            throw new ArgumentException("Seat number must be greater than 0.", nameof(student.SeatNumber));
    }

    /// <summary>
    /// Validate email format
    /// </summary>
    /// <param name="email">Email to validate</param>
    /// <returns>True if email is valid, false otherwise</returns>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate phone number format (basic validation for 10 digits)
    /// </summary>
    /// <param name="phoneNumber">Phone number to validate</param>
    /// <returns>True if phone number is valid, false otherwise</returns>
    private bool IsValidPhoneNumber(string phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(phoneNumber) &&
               System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\d{10}$|^\+?\d{1,3}[-.\s]?\d{1,14}$");
    }

    /// <summary>
    /// Validate student details for registration (VdlId not required - auto-generated)
    /// </summary>
    /// <param name="student">Student to validate</param>
    private void ValidateStudentDetailsForRegistration(Student student)
    {
        // VdlId is auto-generated, so no validation needed
        
        if (string.IsNullOrWhiteSpace(student.Name))
            throw new ArgumentException("Student name is required.", nameof(student.Name));

        if (string.IsNullOrWhiteSpace(student.Email))
            throw new ArgumentException("Email is required.", nameof(student.Email));

        if (!IsValidEmail(student.Email))
            throw new ArgumentException("Email format is invalid.", nameof(student.Email));

        if (string.IsNullOrWhiteSpace(student.FatherName))
            throw new ArgumentException("Father's name is required.", nameof(student.FatherName));

        if (student.DateOfBirth == default)
            throw new ArgumentException("Date of birth is required.", nameof(student.DateOfBirth));

        if (student.DateOfBirth >= DateTime.Now)
            throw new ArgumentException("Date of birth must be in the past.", nameof(student.DateOfBirth));

        if (string.IsNullOrWhiteSpace(student.Gender))
            throw new ArgumentException("Gender is required.", nameof(student.Gender));

        if (string.IsNullOrWhiteSpace(student.Address))
            throw new ArgumentException("Address is required.", nameof(student.Address));

        if (string.IsNullOrWhiteSpace(student.MobileNumber))
            throw new ArgumentException("Mobile number is required.", nameof(student.MobileNumber));

        if (!IsValidPhoneNumber(student.MobileNumber))
            throw new ArgumentException("Mobile number format is invalid.", nameof(student.MobileNumber));

        if (!string.IsNullOrWhiteSpace(student.AlternateNumber) && !IsValidPhoneNumber(student.AlternateNumber))
            throw new ArgumentException("Alternate number format is invalid.", nameof(student.AlternateNumber));

        if (string.IsNullOrWhiteSpace(student.Class))
            throw new ArgumentException("Class is required.", nameof(student.Class));

        if (string.IsNullOrWhiteSpace(student.IdProof))
            throw new ArgumentException("ID proof is required.", nameof(student.IdProof));

        if (string.IsNullOrWhiteSpace(student.ShiftType))
            throw new ArgumentException("Shift type is required.", nameof(student.ShiftType));

        if (student.SeatNumber <= 0)
            throw new ArgumentException("Seat number must be greater than 0.", nameof(student.SeatNumber));
    }
}
