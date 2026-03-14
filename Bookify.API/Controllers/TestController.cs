using Microsoft.AspNetCore.Mvc;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        /// <summary>
        /// Simple ping endpoint for testing that the backend is running. No authentication required.
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                message = "pong",
                timestamp = DateTime.UtcNow.ToString("O"),
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            });
        }
    }
}
