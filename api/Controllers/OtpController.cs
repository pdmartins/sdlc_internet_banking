using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace InternetBankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OtpController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> OtpStore = new();

        [HttpPost("send-otp")]
        public IActionResult SendOtp([FromBody] SendOtpRequest request)
        {
            if (string.IsNullOrEmpty(request.EmailOrPhone))
            {
                return BadRequest(new { message = "Email or phone is required." });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(5);

            OtpStore[request.EmailOrPhone] = (otp, expiry);

            // Simulate sending OTP (e.g., via email or SMS)
            Console.WriteLine($"OTP for {request.EmailOrPhone}: {otp}");

            return Ok(new { message = "OTP sent successfully." });
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrEmpty(request.EmailOrPhone) || string.IsNullOrEmpty(request.Otp))
            {
                return BadRequest(new { message = "Email/phone and OTP are required." });
            }

            if (OtpStore.TryGetValue(request.EmailOrPhone, out var storedOtp) && storedOtp.Otp == request.Otp)
            {
                if (DateTime.UtcNow <= storedOtp.Expiry)
                {
                    OtpStore.TryRemove(request.EmailOrPhone, out _);
                    return Ok(new { message = "OTP verified successfully." });
                }
                else
                {
                    return BadRequest(new { message = "OTP has expired." });
                }
            }

            return BadRequest(new { message = "Invalid OTP." });
        }

        public class SendOtpRequest
        {
            public string EmailOrPhone { get; set; }
        }

        public class VerifyOtpRequest
        {
            public string EmailOrPhone { get; set; }
            public string Otp { get; set; }
        }
    }
}
