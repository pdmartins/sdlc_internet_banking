using System;
using System.ComponentModel.DataAnnotations;

namespace InternetBankingAPI.Models
{
    public class IdentityVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string DocumentPath { get; set; }

        [Required]
        public string SelfiePath { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
