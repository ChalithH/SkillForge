using Microsoft.AspNetCore.Http;
using SkillForge.Api.DTOs.Auth;
using SkillForge.Api.Models;

namespace SkillForge.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        string GenerateJwtToken(User user);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> UpdateProfileAsync(int userId, UpdateProfileDto updateProfileDto);
        Task<string?> SaveProfileImageAsync(IFormFile image, int userId);
    }
}