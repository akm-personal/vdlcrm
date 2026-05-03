using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vdlcrm.Model;

namespace Vdlcrm.Services;

public class FeeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FeeService> _logger;

    public FeeService(AppDbContext context, ILogger<FeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new fee record for a student
    /// </summary>
    public async Task<FeeRecord> CreateFeeRecordAsync(int studentId, decimal totalFee, DateTime startDate, DateTime endDate, string description, int userId)
    {
        var studentExists = await _context.StudentDetails.AnyAsync(s => s.Id == studentId);
        if (!studentExists)
        {
            throw new ArgumentException("Student not found.");
        }

        var record = new FeeRecord
        {
            StudentId = studentId,
            TotalFee = totalFee,
            StartDate = startDate,
            EndDate = endDate,
            Description = description,
            Status = FeeStatus.Pending,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.FeeRecords.Add(record);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Created Fee Record for Student {studentId} with Total Fee {totalFee}");
        return record;
    }

    /// <summary>
    /// Add a fee payment and auto-update the fee record status
    /// </summary>
    public async Task<FeePayment> AddFeePaymentAsync(int feeRecordId, decimal amountPaid, string paymentMode, string note, int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var feeRecord = await _context.FeeRecords
                .Include(f => f.FeePayments)
                .FirstOrDefaultAsync(f => f.Id == feeRecordId);

            if (feeRecord == null)
            {
                throw new ArgumentException("Fee record not found.");
            }

            decimal totalPaidSoFar = feeRecord.FeePayments.Sum(p => p.AmountPaid);
            decimal remainingBalance = feeRecord.TotalFee - totalPaidSoFar;

            if (amountPaid > remainingBalance)
            {
                throw new InvalidOperationException($"Payment amount ({amountPaid}) exceeds the remaining balance ({remainingBalance}).");
            }

            var payment = new FeePayment
            {
                FeeRecordId = feeRecordId,
                AmountPaid = amountPaid,
                PaymentMode = paymentMode,
                Note = note,
                CollectedBy = userId,
                PaymentDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            };
            _context.FeePayments.Add(payment);

            // Auto-update FeeRecord Status
            decimal newTotalPaid = totalPaidSoFar + amountPaid;
            feeRecord.Status = newTotalPaid >= feeRecord.TotalFee ? FeeStatus.Paid : FeeStatus.Partial;
            feeRecord.UpdatedDate = DateTime.UtcNow;
            
            _context.FeeRecords.Update(feeRecord);
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Added Payment of {amountPaid} to Fee Record {feeRecordId}. New Status: {feeRecord.Status}");
            return payment;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError($"Error adding fee payment: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Fetch complete fee balance summary for a student
    /// </summary>
    public async Task<(decimal TotalFee, decimal TotalPaid, decimal Balance)> GetStudentFeeBalanceAsync(int studentId)
    {
        var feeRecords = await _context.FeeRecords
            .Include(f => f.FeePayments)
            .Where(f => f.StudentId == studentId)
            .ToListAsync();

        decimal totalFee = feeRecords.Sum(f => f.TotalFee);
        decimal totalPaid = feeRecords.Sum(f => f.FeePayments.Sum(p => p.AmountPaid));
        decimal balance = totalFee - totalPaid;

        return (totalFee, totalPaid, balance);
    }
    
    /// <summary>
    /// Get all fee records for a student
    /// </summary>
    public async Task<List<FeeRecord>> GetFeeRecordsByStudentAsync(int studentId)
    {
        return await _context.FeeRecords
            .Include(f => f.FeePayments)
            .Where(f => f.StudentId == studentId)
            .OrderByDescending(f => f.CreatedDate)
            .ToListAsync();
    }
}