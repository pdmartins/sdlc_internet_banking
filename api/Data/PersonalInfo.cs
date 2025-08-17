using System;
using System.ComponentModel.DataAnnotations;

namespace InternetBankingAPI.Data
{
    public class PersonalInfo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Address { get; set; }
    }
}
