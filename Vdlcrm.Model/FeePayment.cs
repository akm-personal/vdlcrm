using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Vdlcrm.Model;

public class FeePayment
{
    public int Id { get; set; }
    
    public int FeeRecordId { get; set; }
    [JsonIgnore]
    public virtual FeeRecord FeeRecord { get; set; }
    
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string PaymentMode { get; set; }
    
    public string? CollectedBy { get; set; }

    [NotMapped]
    public string? CollectedByName { get; set; }
    
    [NotMapped]
    public string? CollectedByVdlId { get; set; }
    
    public string? Note { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}