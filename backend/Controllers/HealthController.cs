using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillForge.Api.Data;

namespace SkillForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> Get()
        {
            var result = new 
            {
                status = "healthy",
                service = "SkillForge API", 
                timestamp = DateTime.UtcNow,
                version = "1.0.1",
                database = "unknown"  // Default value
            };

            try 
            {
                await _context.Database.CanConnectAsync();
                result = new { result.status, result.service, result.timestamp, result.version, database = "connected" };
            }
            catch (Exception ex)
            {
                // Keep default "unknown" value for database
                result = new { result.status, result.service, result.timestamp, result.version, database = "disconnected" };
            }

            return Ok(result);
        }
    }
}