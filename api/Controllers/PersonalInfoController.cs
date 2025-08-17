using InternetBankingAPI.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace InternetBankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonalInfoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PersonalInfoController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("submit-info")]
        public IActionResult SubmitPersonalInfo([FromBody] PersonalInfoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data." });
            }

            var personalInfo = new PersonalInfo
            {
                Name = request.Name,
                DateOfBirth = DateTime.Parse(request.DateOfBirth),
                Address = request.Address
            };

            _context.PersonalInfos.Add(personalInfo);
            _context.SaveChanges();

            return Ok(new { message = "Personal information submitted successfully." });
        }

        public class PersonalInfoRequest
        {
            [Required]
            public string Name { get; set; }

            [Required]
            [DataType(DataType.Date)]
            public string DateOfBirth { get; set; }

            [Required]
            public string Address { get; set; }
        }
    }
}
