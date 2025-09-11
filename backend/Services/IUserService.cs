using SkillForge.Api.Models;

namespace SkillForge.Api.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> UpdateUserAsync(int userId, User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<IEnumerable<UserSkill>> GetUserSkillsAsync(int userId);
        Task<UserSkill?> AddUserSkillAsync(int userId, UserSkill userSkill);
        Task<UserSkill?> UpdateUserSkillAsync(int userId, int userSkillId, UserSkill userSkill);
        Task<bool> DeleteUserSkillAsync(int userId, int userSkillId);
    }
}