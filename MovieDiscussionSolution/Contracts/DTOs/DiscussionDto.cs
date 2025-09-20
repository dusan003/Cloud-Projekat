using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs
{
    public class DiscussionDto
    {
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public string Genre { get; set; } = string.Empty;
        public double ImdbRating { get; set; }
        public string Synopsis { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // statistika
        public int PositiveReactions { get; set; }
        public int NegativeReactions { get; set; }
        public int CommentsCount { get; set; }
    }
}
