using System;
using System.Collections.Generic;

namespace Vdlcrm.Model;

public class FeeRecord
{
    public int Id { get; set; }
    
    public int StudentId { get; set; }
    public virtual Student Student { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalFee { get; set; }
    
    public FeeStatus Status { get; set; } = FeeStatus.Pending;
    public string Description { get; set; }
    
    public int? CreatedBy { get; set; }
    public virtual User CreatedByUser { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();
}