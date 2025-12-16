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

            // For rating filter, we need to fetch all matching users first
            var allUsers = await query.ToListAsync();

            // Apply rating filter in memory
            if (minRating.HasValue)
            {
                allUsers = allUsers.Where(u => 
                {
                    var avgRating = u.ReviewsReceived.Any() 
                        ? u.ReviewsReceived.Average(r => r.Rating) 
                        : 0.0;
                    return avgRating >= minRating.Value;
                }).ToList();
            }

            // Apply online filter if specified
            if (isOnline.HasValue)
            {
                var filteredUsers = new List<User>();
                foreach (var user in allUsers)
                {
                    var isUserOnline = await _userPresenceService.IsUserOnlineAsync(user.Id);
                    if (isUserOnline == isOnline.Value)
                    {
                        filteredUsers.Add(user);
                    }
                }
                allUsers = filteredUsers;
            }

            // Get total count after all filters
            var totalCount = allUsers.Count;

            // Apply pagination
            var users = allUsers
                .Skip(offset)
                .Take(limit)
                .ToList();

            // Convert to DTOs and add online status
            var userMatches = new List<UserMatchDto>();
            foreach (var u in users)
            {
                var avgRating = u.ReviewsReceived.Any() 
                    ? u.ReviewsReceived.Average(r => r.Rating) 
                    : 0.0;

                var isUserOnline = await _userPresenceService.IsUserOnlineAsync(u.Id);
                
                var dto = new UserMatchDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Bio = u.Bio,
                    ProfileImageUrl = u.ProfileImageUrl,
                    TimeCredits = u.TimeCredits,
                    Rating = avgRating,
                    AverageRating = avgRating,
                    ReviewCount = u.ReviewsReceived.Count,
                    IsOnline = isUserOnline,
                    SkillsOffered = u.UserSkills.Where(us => us.IsOffering).Select(us => new MatchUserSkillDto
                    {
                        Id = us.Id,
                        SkillId = us.SkillId,
                        SkillName = us.Skill.Name,
                        SkillCategory = us.Skill.Category,
                        ProficiencyLevel = us.ProficiencyLevel,
                        Description = us.Description
                    }).ToList()
                };

                userMatches.Add(dto);
            }

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
                AverageRating = avgRating,
                ReviewCount = user.ReviewsReceived.Count,
                IsOnline = _userPresenceService.IsUserOnlineAsync(user.Id).GetAwaiter().GetResult(),
                SkillsOffered = user.UserSkills.Where(us => us.IsOffering).Select(us => new MatchUserSkillDto
                {
                    Id = us.Id,
                    SkillId = us.SkillId,
                    SkillName = us.Skill.Name,
                    SkillCategory = us.Skill.Category,
                    ProficiencyLevel = us.ProficiencyLevel,
                    Description = us.Description
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

            // Get user's offered skills for mutual match calculation
            var userOfferedSkills = await _context.UserSkills
                .Where(us => us.UserId == userId && us.IsOffering)
                .Select(us => us.SkillId)
                .ToListAsync();

            // Find users who offer skills the current user wants to learn
            var recommendedUsers = await _context.Users
                .Where(u => u.Id != userId)
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.ReviewsReceived)
                .Where(u => u.UserSkills.Any(us =>
                    userLearningInterests.Contains(us.SkillId) && us.IsOffering))
                .ToListAsync();

            // Calculate compatibility scores and sort by score
            var scoredUsers = recommendedUsers.Select(u => {
                var avgRating = u.ReviewsReceived.Any() ? u.ReviewsReceived.Average(r => r.Rating) : 0.0;

                // Calculate compatibility score
                var compatibilityScore = CalculateCompatibilityScore(
                    u,
                    userLearningInterests,
                    userOfferedSkills,
                    avgRating);

                return new UserMatchDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Bio = u.Bio,
                    ProfileImageUrl = u.ProfileImageUrl,
                    TimeCredits = u.TimeCredits,
                    Rating = avgRating,
                    AverageRating = avgRating,
                    ReviewCount = u.ReviewsReceived.Count,
                    CompatibilityScore = compatibilityScore,
                    IsOnline = _userPresenceService.IsUserOnlineAsync(u.Id).GetAwaiter().GetResult(),
                    SkillsOffered = u.UserSkills.Where(us => us.IsOffering).Select(us => new MatchUserSkillDto
                    {
                        Id = us.Id,
                        SkillId = us.SkillId,
                        SkillName = us.Skill.Name,
                        SkillCategory = us.Skill.Category,
                        ProficiencyLevel = us.ProficiencyLevel,
                        Description = us.Description
                    }).ToList()
                };
            })
            .OrderByDescending(u => u.CompatibilityScore)
            .Take(limit);

            return scoredUsers;
        }

        private double CalculateCompatibilityScore(
            User otherUser,
            List<int> userLearningInterests,
            List<int> userOfferedSkills,
            double otherUserRating)
        {
            // Base score starts at 0
            double score = 0;

            // Skills they can teach you (what they offer that you want to learn)
            var theirMatchingSkills = otherUser.UserSkills
                .Where(us => us.IsOffering && userLearningInterests.Contains(us.SkillId))
                .ToList();

            // Skills you can teach them (what they want to learn that you offer)
            var mutualMatchSkills = otherUser.UserSkills
                .Where(us => !us.IsOffering && userOfferedSkills.Contains(us.SkillId))
                .ToList();

            if (theirMatchingSkills.Count == 0)
            {
                return 0;
            }

            // Skill match score (40% weight) - based on how many of your interests they cover
            double skillCoverage = (double)theirMatchingSkills.Count / userLearningInterests.Count;
            score += skillCoverage * 40;

            // Proficiency score (25% weight) - average proficiency of their matching skills
            double avgProficiency = theirMatchingSkills.Average(s => GetProficiencyValue(s.ProficiencyLevel));
            score += (avgProficiency / 3.0) * 25; // Normalize to 0-25 (Expert=3)

            // Mutual exchange potential (20% weight) - they want to learn what you teach
            if (mutualMatchSkills.Count > 0 && userOfferedSkills.Count > 0)
            {
                double mutualCoverage = Math.Min(1.0, (double)mutualMatchSkills.Count / userOfferedSkills.Count);
                score += mutualCoverage * 20;
            }

            // Rating bonus (15% weight)
            score += (otherUserRating / 5.0) * 15;

            return Math.Round(score, 1);
        }

        private int GetProficiencyValue(int proficiencyLevel)
        {
            // ProficiencyLevel is 1-5 in the database
            // Normalize to 1-3 scale for scoring (1=beginner, 2=intermediate, 3=expert)
            return proficiencyLevel switch
            {
                >= 4 => 3,  // 4-5 = Expert
                >= 2 => 2,  // 2-3 = Intermediate
                _ => 1      // 1 = Beginner
            };
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

            return topUsers.Select(u => {
                var avgRating = u.ReviewsReceived.Average(r => r.Rating);
                return new UserMatchDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Bio = u.Bio,
                    ProfileImageUrl = u.ProfileImageUrl,
                    TimeCredits = u.TimeCredits,
                    Rating = avgRating,
                    AverageRating = avgRating,
                    ReviewCount = u.ReviewsReceived.Count,
                    IsOnline = _userPresenceService.IsUserOnlineAsync(u.Id).GetAwaiter().GetResult(),
                    SkillsOffered = u.UserSkills.Where(us => us.IsOffering).Select(us => new MatchUserSkillDto
                    {
                        Id = us.Id,
                        SkillId = us.SkillId,
                        SkillName = us.Skill.Name,
                        SkillCategory = us.Skill.Category,
                        ProficiencyLevel = us.ProficiencyLevel,
                        Description = us.Description
                    }).ToList()
                };
            });
        }
    }
}