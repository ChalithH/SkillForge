using SkillForge.Api.Models;

namespace SkillForge.Api.Services
{
    public interface ISkillService
    {
        Task<IEnumerable<Skill>> GetAllSkillsAsync();
        Task<Skill?> GetSkillByIdAsync(int skillId);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<IEnumerable<Skill>> GetSkillsByCategoryAsync(string category);
        Task<IEnumerable<Skill>> SearchSkillsAsync(string searchTerm);
        Task<Skill?> CreateSkillAsync(Skill skill);
        Task<Skill?> UpdateSkillAsync(int skillId, Skill skill);
        Task<bool> DeleteSkillAsync(int skillId);
    }
}