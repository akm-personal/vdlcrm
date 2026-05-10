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
    public async Task<FeeRecord> CreateFeeRecordAsync(CreateFeeRecordRequest request, string vdlId)
        {
            var student = await _context.StudentDetails.FirstOrDefaultAsync(s => s.VdlId == request.VdlId);
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
                    VdlId = request.VdlId,
                    TotalFee = request.TotalFee,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Status = status,
                    Description = request.Description,
                CreatedBy = vdlId,
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
                    CollectedBy = vdlId,
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

    public async Task<FeePayment> AddFeePaymentAsync(int feeRecordId, decimal amountPaid, string paymentMode, string? note, string vdlId)
        {
            // Implementation for adding subsequent payments
            var feeRecord = await _context.FeeRecords.Include(fr => fr.FeePayments).FirstOrDefaultAsync(fr => fr.Id == feeRecordId);
            if (feeRecord == null) throw new ArgumentException("Fee record not found.");

            var totalPaid = feeRecord.FeePayments.Sum(p => p.AmountPaid);
            if (amountPaid > (feeRecord.TotalFee - totalPaid)) throw new InvalidOperationException("Payment exceeds balance.");

        var payment = new FeePayment { FeeRecordId = feeRecordId, AmountPaid = amountPaid, PaymentDate = DateTime.UtcNow, PaymentMode = paymentMode, Note = note, CollectedBy = vdlId, CreatedDate = DateTime.UtcNow };
            _context.FeePayments.Add(payment);

            feeRecord.Status = (totalPaid + amountPaid) >= feeRecord.TotalFee ? FeeStatus.Paid : FeeStatus.Partial;
            feeRecord.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<(decimal TotalFee, decimal TotalPaid, decimal Balance)> GetStudentFeeBalanceAsync(string vdlId)
        {
            var totalFee = await _context.FeeRecords.Where(fr => fr.VdlId == vdlId).SumAsync(fr => fr.TotalFee);
            var totalPaid = await _context.FeePayments.Where(fp => fp.FeeRecord.VdlId == vdlId).SumAsync(fp => fp.AmountPaid);
            return (totalFee, totalPaid, totalFee - totalPaid);
        }

        public async Task<IEnumerable<FeeRecord>> GetFeeRecordsByStudentAsync(string vdlId)
        {
            var records = await _context.FeeRecords
                .Where(fr => fr.VdlId == vdlId)
                .Include(fr => fr.FeePayments)
                .OrderByDescending(fr => fr.CreatedDate)
                .ToListAsync();

        // Collect unique user VDL IDs to fetch their details efficiently
        var userVdlIds = records.Select(r => r.CreatedBy)
                .Concat(records.SelectMany(r => r.FeePayments.Select(p => p.CollectedBy)))
            .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var users = await _context.Users
            .Where(u => userVdlIds.Contains(u.Username))
                .ToListAsync();

            var userEmails = users.Select(u => u.Email).ToList();
            var userUsernames = users.Select(u => u.Username).ToList();
            
            var studentDetails = await _context.StudentDetails
                .Where(s => userEmails.Contains(s.Email) || userUsernames.Contains(s.VdlId))
                .ToListAsync();

        string GetNameForVdlId(string? collectedOrCreatedByVdlId)
            {
            if (string.IsNullOrWhiteSpace(collectedOrCreatedByVdlId)) return "Unknown";
            var user = users.FirstOrDefault(u => u.Username == collectedOrCreatedByVdlId);
            if (user == null) return collectedOrCreatedByVdlId;
            var student = studentDetails.FirstOrDefault(s => s.Email == user.Email || s.VdlId == user.Username);

                // अगर स्टूडेंट रिकॉर्ड मिलता है और उसका नाम खाली नहीं है, तो वही नाम इस्तेमाल करें।
                if (student != null && !string.IsNullOrWhiteSpace(student.Name))
                {
                    return student.Name;
                }

                // अगर स्टूडेंट रिकॉर्ड नहीं मिलता, या स्टूडेंट का नाम खाली है,
                // तो यूजर का Username इस्तेमाल करें (जो कि VDL ID हो सकता है)।
                // अगर Username भी खाली है, तो "Unknown User" दिखाएं।
            return string.IsNullOrWhiteSpace(user.Username) ? "Unknown User" : user.Username;
            }

            foreach (var record in records)
            {
            if (!string.IsNullOrWhiteSpace(record.CreatedBy))
                {
                record.CreatedByName = GetNameForVdlId(record.CreatedBy);
                record.CreatedByVdlId = record.CreatedBy;
                }

                foreach (var payment in record.FeePayments)
                {
                if (!string.IsNullOrWhiteSpace(payment.CollectedBy))
                    {
                    payment.CollectedByName = GetNameForVdlId(payment.CollectedBy);
                    payment.CollectedByVdlId = payment.CollectedBy;
                    }
                }
            }

            return records;
        }
    }
}