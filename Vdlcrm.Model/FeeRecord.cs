using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Vdlcrm.Model;

public class FeeRecord
{
    public int Id { get; set; }
    
    public string VdlId { get; set; } = string.Empty;
    [JsonIgnore]
    public virtual Student Student { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalFee { get; set; }
    
    public FeeStatus Status { get; set; } = FeeStatus.Pending;
    public string? Description { get; set; }
    
    public string? CreatedBy { get; set; }

    [NotMapped]
    public string? CreatedByName { get; set; }
    
    [NotMapped]
    public string? CreatedByVdlId { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();
}