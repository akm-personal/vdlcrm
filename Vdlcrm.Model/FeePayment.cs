using System;

namespace Vdlcrm.Model;

public class FeePayment
{
    public int Id { get; set; }
    
    public int FeeRecordId { get; set; }
    public virtual FeeRecord FeeRecord { get; set; }
    
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string PaymentMode { get; set; }
    
    public int? CollectedBy { get; set; }
    public virtual User CollectedByUser { get; set; }
    
    public string Note { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}