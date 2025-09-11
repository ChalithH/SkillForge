using Microsoft.EntityFrameworkCore;
using SkillForge.Api.Data;
using SkillForge.Api.DTOs;
using SkillForge.Api.Models;

namespace SkillForge.Api.Services
{
    public class MatchingService : IMatchingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserPresenceService _userPresenceService;
        private readonly ILogger<MatchingService> _logger;

        public MatchingService(
            ApplicationDbContext context, 
            IUserPresenceService userPresenceService,
            ILogger<MatchingService> logger)
        {
            _context = context;
            _userPresenceService = userPresenceService;
            _logger = logger;
        }

        public async Task<PagedResult<UserMatchDto>> BrowseUsersAsync(
            int currentUserId,
            string? category = null,
            double? minRating = null,
            bool? isOnline = null,
            string? skillName = null,
            int page = 1,
            int limit = 20)
        {
            if (limit > 50) limit = 50; // Cap the limit
            var offset = (page - 1) * limit;

            var query = _context.Users
                .Where(u => u.Id != currentUserId)
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.ReviewsReceived)
                .AsQueryable();

            // Filter by skill category
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(u => u.UserSkills.Any(us => 
                    us.Skill.Category == category && us.IsOffering));
            }

            // Filter by specific skill name
            if (!string.IsNullOrEmpty(skillName))
            {
                query = query.Where(u => u.UserSkills.Any(us => 
                    us.Skill.Name.Contains(skillName) && us.IsOffering));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Get the users
            var users = await query
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            // Convert to DTOs and add online status
            var userMatches = users.Select(u =>
            {
                var avgRating = u.ReviewsReceived.Any() 
                    ? u.ReviewsReceived.Average(r => r.Rating) 
                    : 0.0;

                // Apply minimum rating filter after calculating average
                if (minRating.HasValue && avgRating < minRating.Value)
                {
                    return null;
                }

                var dto = new UserMatchDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Bio = u.Bio,
                    ProfileImageUrl = u.ProfileImageUrl,
                    TimeCredits = u.TimeCredits,
                    Rating = avgRating,
                    ReviewCount = u.ReviewsReceived.Count,
                    IsOnline = _userPresenceService.IsUserOnlineAsync(u.Id).GetAwaiter().GetResult(),
                    Skills = u.UserSkills.Where(us => us.IsOffering).Select(us => new UserSkillDto
                    {
                        Id = us.Id,
                        UserId = us.UserId,
                        SkillId = us.SkillId,
                        ProficiencyLevel = us.ProficiencyLevel,
                        IsOffering = us.IsOffering,
                        Description = us.Description,
                        Skill = new SkillDto
                        {
                            Id = us.Skill.Id,
                            Name = us.Skill.Name,
                            Category = us.Skill.Category,
                            Description = us.Skill.Description
                        }
                    }).ToList()
                };

                // Apply online filter after determining status
                if (isOnline.HasValue && dto.IsOnline != isOnline.Value)
                {
                    return null;
                }

                return dto;
            })
            .Where(dto => dto != null)
            .Cast<UserMatchDto>()
            .ToList();

            return new PagedResult<UserMatchDto>
            {
                Items = userMatches,
                TotalCount = totalCount,
                Page = page,
                PageSize = limit,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };
        }

        public async Task<UserMatchDto?> GetUserMatchDetailsAsync(int userId, int currentUserId)
        {
            var user = await _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.ReviewsReceived)
                    .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            var avgRating = user.ReviewsReceived.Any() 
                ? user.ReviewsReceived.Average(r => r.Rating) 
                : 0.0;

            return new UserMatchDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                TimeCredits = user.TimeCredits,
                Rating = avgRating,
                ReviewCount = user.ReviewsReceived.Count,
                IsOnline = _userPresenceService.IsUserOnlineAsync(user.Id).GetAwaiter().GetResult(),
                Skills = user.UserSkills.Select(us => new UserSkillDto
                {
                    Id = us.Id,
                    UserId = us.UserId,
                    SkillId = us.SkillId,
                    ProficiencyLevel = us.ProficiencyLevel,
                    IsOffering = us.IsOffering,
                    Description = us.Description,
                    Skill = new SkillDto
                    {
                        Id = us.Skill.Id,
                        Name = us.Skill.Name,
                        Category = us.Skill.Category,
                        Description = us.Skill.Description
                    }
                }).ToList()
            };
        }

        public async Task<IEnumerable<UserMatchDto>> GetRecommendedMatchesAsync(int userId, int limit = 10)
        {
            // Get user's learning interests (skills they're not offering)
            var userLearningInterests = await _context.UserSkills
                .Where(us => us.UserId == userId && !us.IsOffering)
                .Select(us => us.SkillId)
                .ToListAsync();

            if (!userLearningInterests.Any())
            {
                // If user has no learning interests, return top-rated users
                return await GetTopRatedUsersAsync(null, limit);
            }

            // Find users who offer skills the current user wants to learn
            var recommendedUsers = await _context.Users
                .Where(u => u.Id != userId)
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.ReviewsReceived)
                .Where(u => u.UserSkills.Any(us => 
                    userLearningInterests.Contains(us.SkillId) && us.IsOffering))
                .Take(limit)
                .ToListAsync();

            return recommendedUsers.Select(u => new UserMatchDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Bio = u.Bio,
                ProfileImageUrl = u.ProfileImageUrl,
                TimeCredits = u.TimeCredits,
                Rating = u.ReviewsReceived.Any() ? u.ReviewsReceived.Average(r => r.Rating) : 0.0,
                ReviewCount = u.ReviewsReceived.Count,
                IsOnline = _userPresenceService.IsUserOnlineAsync(u.Id).GetAwaiter().GetResult(),
                Skills = u.UserSkills.Where(us => us.IsOffering).Select(us => new UserSkillDto
                {
                    Id = us.Id,
                    UserId = us.UserId,
                    SkillId = us.SkillId,
                    ProficiencyLevel = us.ProficiencyLevel,
                    IsOffering = us.IsOffering,
                    Description = us.Description,
                    Skill = new SkillDto
                    {
                        Id = us.Skill.Id,
                        Name = us.Skill.Name,
                        Category = us.Skill.Category,
                        Description = us.Skill.Description
                    }
                }).ToList()
            });
        }

        public async Task<IEnumerable<UserMatchDto>> GetTopRatedUsersAsync(string? category = null, int limit = 10)
        {
            var query = _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.ReviewsReceived)
                .Where(u => u.ReviewsReceived.Any()) // Only users with reviews
                .AsQueryable();

            // Filter by category if specified
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(u => u.UserSkills.Any(us => 
                    us.Skill.Category == category && us.IsOffering));
            }

            var topUsers = await query
                .OrderByDescending(u => u.ReviewsReceived.Average(r => r.Rating))
                .ThenByDescending(u => u.ReviewsReceived.Count)
                .Take(limit)
                .ToListAsync();

            return topUsers.Select(u => new UserMatchDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Bio = u.Bio,
                ProfileImageUrl = u.ProfileImageUrl,
                TimeCredits = u.TimeCredits,
                Rating = u.ReviewsReceived.Average(r => r.Rating),
                ReviewCount = u.ReviewsReceived.Count,
                IsOnline = _userPresenceService.IsUserOnlineAsync(u.Id).GetAwaiter().GetResult(),
                Skills = u.UserSkills.Where(us => us.IsOffering).Select(us => new UserSkillDto
                {
                    Id = us.Id,
                    UserId = us.UserId,
                    SkillId = us.SkillId,
                    ProficiencyLevel = us.ProficiencyLevel,
                    IsOffering = us.IsOffering,
                    Description = us.Description,
                    Skill = new SkillDto
                    {
                        Id = us.Skill.Id,
                        Name = us.Skill.Name,
                        Category = us.Skill.Category,
                        Description = us.Skill.Description
                    }
                }).ToList()
            });
        }
    }
}