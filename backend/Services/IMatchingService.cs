using SkillForge.Api.DTOs;

namespace SkillForge.Api.Services
{
    public interface IMatchingService
    {
        Task<PagedResult<UserMatchDto>> BrowseUsersAsync(
            int currentUserId,
            string? category = null,
            double? minRating = null,
            bool? isOnline = null,
            string? skillName = null,
            int page = 1,
            int limit = 20);
        Task<UserMatchDto?> GetUserMatchDetailsAsync(int userId, int currentUserId);
        Task<IEnumerable<UserMatchDto>> GetRecommendedMatchesAsync(int userId, int limit = 10);
        Task<IEnumerable<UserMatchDto>> GetTopRatedUsersAsync(string? category = null, int limit = 10);
    }
}