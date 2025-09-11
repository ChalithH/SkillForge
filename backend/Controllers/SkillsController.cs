using Microsoft.AspNetCore.Mvc;
using SkillForge.Api.Models;
using SkillForge.Api.Services;

namespace SkillForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillsController : ControllerBase
    {
        private readonly ISkillService _skillService;

        public SkillsController(ISkillService skillService)
        {
            _skillService = skillService;
        }

        // GET: api/skills
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
        {
            var skills = await _skillService.GetAllSkillsAsync();
            return Ok(skills);
        }

        // GET: api/skills/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Skill>> GetSkill(int id)
        {
            var skill = await _skillService.GetSkillByIdAsync(id);

            if (skill == null)
            {
                return NotFound();
            }

            return Ok(skill);
        }

        // GET: api/skills/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _skillService.GetCategoriesAsync();
            return Ok(categories);
        }

        // GET: api/skills/category/{category}
        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<Skill>>> GetSkillsByCategory(string category)
        {
            var skills = await _skillService.GetSkillsByCategoryAsync(category);

            if (!skills.Any())
            {
                return NotFound($"No skills found in category '{category}'");
            }

            return Ok(skills);
        }

    }
}