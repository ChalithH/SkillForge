using Microsoft.EntityFrameworkCore;
using SkillForge.Api.Data;
using SkillForge.Api.Models;

namespace SkillForge.Api.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.ReviewsReceived)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.UserSkills)
                    .ThenInclude(us => us.Skill)
                .Include(u => u.ReviewsReceived)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<User?> UpdateUserAsync(int userId, User user)
        {
            var existingUser = await _context.Users.FindAsync(userId);
            if (existingUser == null)
            {
                return null;
            }

            // Update only allowed fields
            existingUser.Name = user.Name;
            existingUser.Bio = user.Bio;
            existingUser.ProfileImageUrl = user.ProfileImageUrl;
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingUser;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.ReviewsReceived)
                .Include(u => u.ReviewsGiven)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null)
            {
                return false;
            }

            // Remove related reviews first due to foreign key constraints
            _context.Reviews.RemoveRange(user.ReviewsReceived);
            _context.Reviews.RemoveRange(user.ReviewsGiven);
            
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserSkill>> GetUserSkillsAsync(int userId)
        {
            return await _context.UserSkills
                .Include(us => us.Skill)
                .Where(us => us.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserSkill?> AddUserSkillAsync(int userId, UserSkill userSkill)
        {
            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return null;
            }

            // Verify skill exists
            var skillExists = await _context.Skills.AnyAsync(s => s.Id == userSkill.SkillId);
            if (!skillExists)
            {
                return null;
            }

            // Check if user already has this skill
            var existingUserSkill = await _context.UserSkills
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == userSkill.SkillId);
            
            if (existingUserSkill != null)
            {
                // Update existing skill instead
                existingUserSkill.ProficiencyLevel = userSkill.ProficiencyLevel;
                existingUserSkill.IsOffering = userSkill.IsOffering;
                existingUserSkill.Description = userSkill.Description;
                await _context.SaveChangesAsync();
                return existingUserSkill;
            }

            // Add new user skill
            userSkill.UserId = userId;
            _context.UserSkills.Add(userSkill);
            await _context.SaveChangesAsync();

            // Load related data
            await _context.Entry(userSkill)
                .Reference(us => us.Skill)
                .LoadAsync();

            return userSkill;
        }

        public async Task<UserSkill?> UpdateUserSkillAsync(int userId, int userSkillId, UserSkill userSkill)
        {
            var existingUserSkill = await _context.UserSkills
                .Include(us => us.Skill)
                .FirstOrDefaultAsync(us => us.Id == userSkillId && us.UserId == userId);

            if (existingUserSkill == null)
            {
                return null;
            }

            existingUserSkill.ProficiencyLevel = userSkill.ProficiencyLevel;
            existingUserSkill.IsOffering = userSkill.IsOffering;
            existingUserSkill.Description = userSkill.Description;

            await _context.SaveChangesAsync();
            return existingUserSkill;
        }

        public async Task<bool> DeleteUserSkillAsync(int userId, int userSkillId)
        {
            var userSkill = await _context.UserSkills
                .FirstOrDefaultAsync(us => us.Id == userSkillId && us.UserId == userId);

            if (userSkill == null)
            {
                return false;
            }

            _context.UserSkills.Remove(userSkill);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}