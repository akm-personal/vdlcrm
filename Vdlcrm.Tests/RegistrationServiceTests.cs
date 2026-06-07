using System;
using Xunit;
using Vdlcrm.Services;
using Vdlcrm.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace Vdlcrm.Tests;

public class RegistrationServiceTests
{
    [Fact]
    public async System.Threading.Tasks.Task RegisterStudent_CreatesStudentAndUserAsync()
    {
        var (context, connection) = TestHelpers.CreateContext();
        try
        {
            var logger = new NullLogger<RegistrationService>();
            var service = new RegistrationService(context, logger);

            var student = new Student
            {
                Name = "Test Student",
                Email = "test.student@example.com",
                FatherName = "Parent",
                DateOfBirth = new DateTime(2005,1,1),
                Gender = "Male",
                Address = "123 Test Ave",
                MobileNumber = "1234567890",
                Class = "10",
                IdProof = "ID123",
                ShiftType = "Morning",
                SeatNumber = 1
            };

            var (registeredStudent, user, tempPassword) = await service.RegisterStudentWithUserAsync(student);

            Assert.False(string.IsNullOrEmpty(registeredStudent.VdlId));
            Assert.StartsWith("VDL", registeredStudent.VdlId, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(user);
            Assert.Equal(student.Email.ToLower(), user.Email);
            Assert.False(string.IsNullOrEmpty(tempPassword));
            Assert.False(user.IsPasswordChangedFromTemp);
        }
        finally
        {
            connection.Close();
        }
    }
}
