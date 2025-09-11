using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillForge.Api.DTOs;
using SkillForge.Api.Models;
using SkillForge.Api.Services;
using System.Security.Claims;

namespace SkillForge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserSkillsController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserSkillsController> _logger;

        public UserSkillsController(IUserService userService, ILogger<UserSkillsController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: api/userskills
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserSkillDto>>> GetMySkills()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var userSkills = await _userService.GetUserSkillsAsync(userId);
                
                var userSkillDtos = userSkills.Select(us => new UserSkillDto
                {
                    Id = us.Id,
                    UserId = us.UserId,
                    SkillId = us.SkillId,
                    ProficiencyLevel = us.ProficiencyLevel,
                    IsOffering = us.IsOffering,
                    Description = us.Description,
                    Skill = us.Skill != null ? new SkillDto
                    {
                        Id = us.Skill.Id,
                        Name = us.Skill.Name,
                        Category = us.Skill.Category,
                        Description = us.Skill.Description
                    } : null
                });

                return Ok(userSkillDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user skills");
                return StatusCode(500, new { message = "An error occurred while retrieving skills" });
            }
        }

        // GET: api/userskills/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserSkillDto>> GetUserSkill(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var userSkills = await _userService.GetUserSkillsAsync(userId);
                var userSkill = userSkills.FirstOrDefault(us => us.Id == id);

                if (userSkill == null)
                {
                    return NotFound();
                }

                var userSkillDto = new UserSkillDto
                {
                    Id = userSkill.Id,
                    UserId = userSkill.UserId,
                    SkillId = userSkill.SkillId,
                    ProficiencyLevel = userSkill.ProficiencyLevel,
                    IsOffering = userSkill.IsOffering,
                    Description = userSkill.Description,
                    Skill = userSkill.Skill != null ? new SkillDto
                    {
                        Id = userSkill.Skill.Id,
                        Name = userSkill.Skill.Name,
                        Category = userSkill.Skill.Category,
                        Description = userSkill.Skill.Description
                    } : null
                };

                return Ok(userSkillDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user skill {SkillId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the skill" });
            }
        }

        // POST: api/userskills
        [HttpPost]
        public async Task<ActionResult<UserSkillDto>> CreateUserSkill(CreateUserSkillDto createDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var userSkill = new UserSkill
                {
                    UserId = userId,
                    SkillId = createDto.SkillId,
                    ProficiencyLevel = createDto.ProficiencyLevel,
                    IsOffering = createDto.IsOffering,
                    Description = createDto.Description
                };

                var createdSkill = await _userService.AddUserSkillAsync(userId, userSkill);

                if (createdSkill == null)
                {
                    return BadRequest(new { message = "Failed to add skill. Skill may not exist or is already added." });
                }

                var userSkillDto = new UserSkillDto
                {
                    Id = createdSkill.Id,
                    UserId = createdSkill.UserId,
                    SkillId = createdSkill.SkillId,
                    ProficiencyLevel = createdSkill.ProficiencyLevel,
                    IsOffering = createdSkill.IsOffering,
                    Description = createdSkill.Description,
                    Skill = createdSkill.Skill != null ? new SkillDto
                    {
                        Id = createdSkill.Skill.Id,
                        Name = createdSkill.Skill.Name,
                        Category = createdSkill.Skill.Category,
                        Description = createdSkill.Skill.Description
                    } : null
                };

                return CreatedAtAction(nameof(GetUserSkill), new { id = userSkillDto.Id }, userSkillDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user skill");
                return StatusCode(500, new { message = "An error occurred while adding the skill" });
            }
        }

        // PUT: api/userskills/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserSkill(int id, UpdateUserSkillDto updateDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var userSkill = new UserSkill
                {
                    ProficiencyLevel = updateDto.ProficiencyLevel,
                    IsOffering = updateDto.IsOffering,
                    Description = updateDto.Description
                };

                var updatedSkill = await _userService.UpdateUserSkillAsync(userId, id, userSkill);

                if (updatedSkill == null)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user skill {SkillId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the skill" });
            }
        }

        // DELETE: api/userskills/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserSkill(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var deleted = await _userService.DeleteUserSkillAsync(userId, id);

                if (!deleted)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user skill {SkillId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the skill" });
            }
        }
    }
}