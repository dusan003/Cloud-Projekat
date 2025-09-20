using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.Enums;

namespace Contracts.DTOs
{
    public class HealthCheckDto
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public HealthStatus Status { get; set; }
        public string ServiceName { get; set; } = string.Empty;
    }
}
