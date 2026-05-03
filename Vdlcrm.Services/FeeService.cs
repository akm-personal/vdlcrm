using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vdlcrm.Model;
using Vdlcrm.Model.DTOs;

namespace Vdlcrm.Services
{
    public class FeeService
    {
        private readonly AppDbContext _context;

        public FeeService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a fee record and an initial payment in a single transaction.
        /// </summary>
        public async Task<FeeRecord> CreateFeeRecordAsync(CreateFeeRecordRequest request, int userId)
        {
            var student = await _context.StudentDetails.FindAsync(request.StudentId);
            if (student == null)
            {
                throw new ArgumentException("Student not found.");
            }

            if (request.TotalFee < 0 || request.CollectedFee < 0)
            {
                throw new ArgumentException("Fee amounts cannot be negative.");
            }

            if (request.CollectedFee > request.TotalFee)
            {
                throw new ArgumentException("Collected fee cannot be greater than the total fee.");
            }

            // 1. Calculate Status
            FeeStatus status = FeeStatus.Pending;
            if (request.CollectedFee > 0 && request.CollectedFee < request.TotalFee)
            {
                status = FeeStatus.Partial;
            }
            else if (request.CollectedFee >= request.TotalFee)
            {
                status = FeeStatus.Paid;
            }

            // Use a transaction to ensure data integrity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. Create the Fee Record
                var record = new FeeRecord
                {
                    StudentId = request.StudentId,
                    TotalFee = request.TotalFee,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Status = status,
                    Description = request.Description,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.FeeRecords.Add(record);
                await _context.SaveChangesAsync(); // This saves the record and assigns it an ID

                // 3. If any fee was collected, create a corresponding Fee Payment record
                if (request.CollectedFee > 0)
                {
                    var payment = new FeePayment
                    {
                        FeeRecordId = record.Id, // Use the ID from the newly created record
                        AmountPaid = request.CollectedFee,
                        PaymentDate = DateTime.UtcNow,
                        PaymentMode = request.PaymentMode,
                        Note = request.PaymentNote ?? "Initial payment.",
                        CollectedBy = userId,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.FeePayments.Add(payment);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync(); // Commit the transaction if everything is successful
                return record;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(); // Rollback if any error occurs
                throw; // Re-throw the exception to be handled by the controller
            }
        }

        public async Task<FeePayment> AddFeePaymentAsync(int feeRecordId, decimal amountPaid, string paymentMode, string? note, int userId)
        {
            // Implementation for adding subsequent payments
            var feeRecord = await _context.FeeRecords.Include(fr => fr.FeePayments).FirstOrDefaultAsync(fr => fr.Id == feeRecordId);
            if (feeRecord == null) throw new ArgumentException("Fee record not found.");

            var totalPaid = feeRecord.FeePayments.Sum(p => p.AmountPaid);
            if (amountPaid > (feeRecord.TotalFee - totalPaid)) throw new InvalidOperationException("Payment exceeds balance.");

            var payment = new FeePayment { FeeRecordId = feeRecordId, AmountPaid = amountPaid, PaymentDate = DateTime.UtcNow, PaymentMode = paymentMode, Note = note, CollectedBy = userId, CreatedDate = DateTime.UtcNow };
            _context.FeePayments.Add(payment);

            feeRecord.Status = (totalPaid + amountPaid) >= feeRecord.TotalFee ? FeeStatus.Paid : FeeStatus.Partial;
            feeRecord.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<(decimal TotalFee, decimal TotalPaid, decimal Balance)> GetStudentFeeBalanceAsync(int studentId)
        {
            var totalFee = await _context.FeeRecords.Where(fr => fr.StudentId == studentId).SumAsync(fr => fr.TotalFee);
            var totalPaid = await _context.FeePayments.Where(fp => fp.FeeRecord.StudentId == studentId).SumAsync(fp => fp.AmountPaid);
            return (totalFee, totalPaid, totalFee - totalPaid);
        }

        public async Task<IEnumerable<FeeRecord>> GetFeeRecordsByStudentAsync(int studentId)
        {
            return await _context.FeeRecords
                .Where(fr => fr.StudentId == studentId)
                .Include(fr => fr.FeePayments)
                .OrderByDescending(fr => fr.CreatedDate)
                .ToListAsync();
        }
    }
}