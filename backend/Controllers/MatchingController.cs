using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillForge.Api.DTOs;
using SkillForge.Api.Services;
using System.Security.Claims;

namespace SkillForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchingController : ControllerBase
    {
        private readonly IMatchingService _matchingService;
        private readonly ILogger<MatchingController> _logger;

        public MatchingController(IMatchingService matchingService, ILogger<MatchingController> logger)
        {
            _matchingService = matchingService;
            _logger = logger;
        }

        [HttpGet("browse")]
        public async Task<ActionResult<PagedResult<UserMatchDto>>> BrowseUsers(
            [FromQuery] string? category = null,
            [FromQuery] double? minRating = null,
            [FromQuery] bool? isOnline = null,
            [FromQuery] string? skillName = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                var result = await _matchingService.BrowseUsersAsync(
                    currentUserId, 
                    category, 
                    minRating, 
                    isOnline, 
                    skillName, 
                    page, 
                    limit);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error browsing users");
                return StatusCode(500, new { message = "An error occurred while browsing users" });
            }
        }

        [HttpGet("recommendations")]
        public async Task<ActionResult<IEnumerable<UserMatchDto>>> GetRecommendations([FromQuery] int limit = 10)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                if (limit > 20) limit = 20; // Cap the limit

                var recommendations = await _matchingService.GetRecommendedMatchesAsync(currentUserId, limit);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations");
                return StatusCode(500, new { message = "An error occurred while getting recommendations" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<UserMatchDto>> GetUserMatchDetails(int userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                var userMatch = await _matchingService.GetUserMatchDetailsAsync(userId, currentUserId);
                
                if (userMatch == null)
                {
                    return NotFound("User not found");
                }

                return Ok(userMatch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user match details for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while getting user details" });
            }
        }

        [HttpGet("top-rated")]
        public async Task<ActionResult<IEnumerable<UserMatchDto>>> GetTopRatedUsers(
            [FromQuery] string? category = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (limit > 20) limit = 20; // Cap the limit

                var topRated = await _matchingService.GetTopRatedUsersAsync(category, limit);
                return Ok(topRated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top-rated users");
                return StatusCode(500, new { message = "An error occurred while getting top-rated users" });
            }
        }
    }
}