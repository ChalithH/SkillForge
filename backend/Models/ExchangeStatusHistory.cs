using System.ComponentModel.DataAnnotations;

namespace SkillForge.Api.Models
{
    public class ExchangeStatusHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int ExchangeId { get; set; }
        public SkillExchange? Exchange { get; set; }
        
        // Previous status (null for initial creation)
        public ExchangeStatus? FromStatus { get; set; }
        
        [Required]
        public ExchangeStatus ToStatus { get; set; }
        
        [Required]
        public int ChangedBy { get; set; }
        public User? ChangedByUser { get; set; }
        
        [Required]
        public DateTime ChangedAt { get; set; }
        
        [MaxLength(1000)]
        public string? Reason { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
    }
}