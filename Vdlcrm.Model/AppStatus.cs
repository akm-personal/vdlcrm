using System;

namespace Vdlcrm.Model
{
    public class AppStatus
    {
        public int Id { get; set; }
        public int StatusId { get; set; }
        public string StatusType { get; set; } = null!;
        public string StatusName { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}