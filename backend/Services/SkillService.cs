using Microsoft.EntityFrameworkCore;
using SkillForge.Api.Data;
using SkillForge.Api.Models;

namespace SkillForge.Api.Services
{
    public class SkillService : ISkillService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SkillService> _logger;

        public SkillService(ApplicationDbContext context, ILogger<SkillService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Skill>> GetAllSkillsAsync()
        {
            return await _context.Skills
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Skill?> GetSkillByIdAsync(int skillId)
        {
            return await _context.Skills.FindAsync(skillId);
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _context.Skills
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<IEnumerable<Skill>> GetSkillsByCategoryAsync(string category)
        {
            return await _context.Skills
                .Where(s => s.Category.ToLower() == category.ToLower())
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Skill>> SearchSkillsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllSkillsAsync();
            }

            var lowerSearchTerm = searchTerm.ToLower();
            return await _context.Skills
                .Where(s => s.Name.ToLower().Contains(lowerSearchTerm) ||
                           s.Description.ToLower().Contains(lowerSearchTerm) ||
                           s.Category.ToLower().Contains(lowerSearchTerm))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Skill?> CreateSkillAsync(Skill skill)
        {
            // Check if skill with same name already exists
            var existingSkill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Name == skill.Name);
            
            if (existingSkill != null)
            {
                _logger.LogWarning("Skill with name {SkillName} already exists", skill.Name);
                return null;
            }

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created new skill {SkillId}: {SkillName}", skill.Id, skill.Name);
            return skill;
        }

        public async Task<Skill?> UpdateSkillAsync(int skillId, Skill skill)
        {
            var existingSkill = await _context.Skills.FindAsync(skillId);
            if (existingSkill == null)
            {
                return null;
            }

            // Check if another skill with the same name exists
            if (skill.Name != existingSkill.Name)
            {
                var duplicateSkill = await _context.Skills
                    .FirstOrDefaultAsync(s => s.Name == skill.Name && s.Id != skillId);
                
                if (duplicateSkill != null)
                {
                    _logger.LogWarning("Cannot update skill {SkillId}: Name {SkillName} already exists", 
                        skillId, skill.Name);
                    return null;
                }
            }

            existingSkill.Name = skill.Name;
            existingSkill.Description = skill.Description;
            existingSkill.Category = skill.Category;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated skill {SkillId}", skillId);
            return existingSkill;
        }

        public async Task<bool> DeleteSkillAsync(int skillId)
        {
            var skill = await _context.Skills.FindAsync(skillId);
            if (skill == null)
            {
                return false;
            }

            // Check if skill is being used
            var isUsed = await _context.UserSkills.AnyAsync(us => us.SkillId == skillId);
            if (isUsed)
            {
                _logger.LogWarning("Cannot delete skill {SkillId}: Still in use by users", skillId);
                return false;
            }

            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted skill {SkillId}: {SkillName}", skillId, skill.Name);
            return true;
        }
    }
}