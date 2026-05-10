using System;

namespace Vdlcrm.Model.DTOs;

public class CreateFeeRecordRequest
{
        public string VdlId { get; set; } = string.Empty;
        public decimal TotalFee { get; set; }
        public decimal CollectedFee { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public string? PaymentNote { get; set; }
}

public class AddFeePaymentRequest
{
    public int FeeRecordId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class FeeBalanceResponse
{
    public string VdlId { get; set; } = string.Empty;
    public decimal TotalFee { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
}