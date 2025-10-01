using Microsoft.AspNetCore.Mvc;

namespace OnlineShppingSystem.API.Controllers
{
    [ApiController]
    [Route("billing")]
    public class BillingController : ControllerBase
    {
        [HttpGet("success")]
        public IActionResult Success([FromQuery] string plan, [FromQuery(Name = "return")] string? ret)
        {
           
            return Ok($"Payment success. plan={plan} return={ret}");
        }

        [HttpGet("cancel")]
        public IActionResult Cancel([FromQuery] string? plan) =>
            Ok($"Payment cancelled{(string.IsNullOrWhiteSpace(plan) ? "" : $" for plan={plan}")}.");
    }
}
