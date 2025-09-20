using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.Enums;

namespace Contracts.DTOs
{
    public class ReactionDto
    {
        public Guid Id { get; set; }
        public Guid DiscussionId { get; set; }
        public Guid UserId { get; set; }
        public ReactionType Type { get; set; }   // Positive / Negative
    }
}
