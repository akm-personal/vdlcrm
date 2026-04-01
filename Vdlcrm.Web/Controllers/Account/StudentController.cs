using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vdlcrm.Model;
using Vdlcrm.Model.DTOs;
using Vdlcrm.Services;

namespace Vdlcrm.Web.Controllers.Account;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentController : ControllerBase
{
    private readonly StudentService _studentService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(StudentService studentService, ILogger<StudentController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new student (Public endpoint)
    /// Auto-generates VDL ID (VDL001, VDL002, etc.)
    /// Creates user account with temporary password
    /// Thread-safe for concurrent bulk registrations
    /// </summary>
    /// <param name="student">Student registration details (VdlId not required, will be auto-generated)</param>
    /// <returns>Newly registered student with VDL ID, username, and temp password</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudentRegistrationResponse>> RegisterStudent([FromBody] Student student)
    {
        if (student == null)
        {
            return BadRequest(new { message = "Student details cannot be null." });
        }

        try
        {
            _logger.LogInformation($"Processing student registration for email: {student.Email}");

            // Register student with auto-generated VDL ID and user account creation
            var (registeredStudent, userAccount, tempPassword) = await _studentService.RegisterStudentAsync(student);

            // Prepare response with temp password
            var response = new StudentRegistrationResponse
            {
                StudentId = registeredStudent.Id,
                VdlId = registeredStudent.VdlId,
                Name = registeredStudent.Name,
                Email = registeredStudent.Email,
                Username = userAccount.Username,
                TempPassword = tempPassword,  // Send temp password once to student
                RoleId = userAccount.RoleId,
                CreatedDate = registeredStudent.CreatedDate,
                Message = "Student registered successfully. Use the provided VDL ID, username, and temporary password to login."
            };

            _logger.LogInformation($"Student registration successful: VdlId={registeredStudent.VdlId}, Username={userAccount.Username}");

            return CreatedAtAction(nameof(GetStudentById), new { id = registeredStudent.Id }, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Validation error during student registration: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError($"Database conflict during student registration: {dbEx.Message}");
            return Conflict(new { message = "Student or email already exists. Please use different email.", error = dbEx.Message });
        }
        catch (InvalidOperationException ioEx)
        {
            _logger.LogError($"Operation error during student registration: {ioEx.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Unable to complete registration. Please try again.", error = ioEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error during student registration: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while registering the student.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all registered students (Admin and Internal User only)
    /// </summary>
    /// <returns>List of all students</returns>
    [HttpGet("/api/StudentList")]
    [Authorize(Roles = "Admin,Internal User")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Student>>> GetAllStudents()
    {
        try
        {
            var students = await _studentService.GetAllStudentsAsync();
            return Ok(students);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving students.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a student by ID (Authorized users only)
    /// </summary>
    /// <param name="id">Student ID</param>
    /// <returns>Student details if found</returns>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Student>> GetStudentById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Student ID must be greater than 0." });
        }

        try
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound(new { message = $"Student with ID {id} not found." });
            }

            return Ok(student);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the student.", error = ex.Message });
        }
    }

    /// <summary>
    /// Update student details (Admin only)
    /// </summary>
    /// <param name="id">Student ID</param>
    /// <param name="student">Updated student details</param>
    /// <returns>Updated student</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Student>> UpdateStudent(int id, [FromBody] Student student)
    {
        if (id <= 0 || student == null)
        {
            return BadRequest(new { message = "Invalid student ID or student details." });
        }

        if (student.Id != id)
        {
            return BadRequest(new { message = "Student ID in URL does not match the ID in the request body." });
        }

        try
        {
            var existingStudent = await _studentService.GetStudentByIdAsync(id);
            if (existingStudent == null)
            {
                return NotFound(new { message = $"Student with ID {id} not found." });
            }

            var updatedStudent = await _studentService.UpdateStudentAsync(student);
            return Ok(updatedStudent);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the student.", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a student (Admin only)
    /// </summary>
    /// <param name="id">Student ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteStudent(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Student ID must be greater than 0." });
        }

        try
        {
            var existingStudent = await _studentService.GetStudentByIdAsync(id);
            if (existingStudent == null)
            {
                return NotFound(new { message = $"Student with ID {id} not found." });
            }

            var deleted = await _studentService.DeleteStudentAsync(id);
            if (!deleted)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Failed to delete the student." });
            }

            return Ok(new { message = $"Student with ID {id} has been successfully deleted." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the student.", error = ex.Message });
        }
    }
}
