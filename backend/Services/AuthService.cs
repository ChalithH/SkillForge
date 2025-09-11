using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillForge.Api.Data;
using SkillForge.Api.DTOs.Auth;
using SkillForge.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SkillForge.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                return null;
            }

            // Generate default avatar URL
            var defaultAvatarUrl = GenerateDefaultAvatarUrl(registerDto.Name, registerDto.Email);

            // Create new user
            var user = new User
            {
                Email = registerDto.Email,
                Name = registerDto.Name,
                Bio = registerDto.Bio,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                TimeCredits = 5, // New users start with 5 free credits
                ProfileImageUrl = defaultAvatarUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate token and return response
            var token = GenerateJwtToken(user);
            
            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Name = user.Name,
                Id = user.Id,
                TimeCredits = user.TimeCredits,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl
            };
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            // Generate token and return response
            var token = GenerateJwtToken(user);
            
            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Name = user.Name,
                Id = user.Id,
                TimeCredits = user.TimeCredits,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl
            };
        }

        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"]!);
            var expirationHours = int.Parse(_configuration["JwtSettings:ExpirationInHours"] ?? "24");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Name)
                }),
                Expires = DateTime.UtcNow.AddHours(expirationHours),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        private string GenerateDefaultAvatarUrl(string name, string email)
        {
            // Split name to get first and last name
            var nameParts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? nameParts[^1] : "";
            
            // Create initials
            var initials = $"{firstName.FirstOrDefault()}{lastName.FirstOrDefault()}".ToUpper();
            if (string.IsNullOrEmpty(initials))
            {
                initials = email.FirstOrDefault().ToString().ToUpper();
            }

            // Generate a color based on the user's email (for consistency)
            var colorHash = Math.Abs(email.GetHashCode()) % 16;
            var backgroundColors = new[]
            {
                "3B82F6", // blue
                "EF4444", // red  
                "10B981", // green
                "F59E0B", // amber
                "8B5CF6", // violet
                "EC4899", // pink
                "06B6D4", // cyan
                "84CC16", // lime
                "F97316", // orange
                "6366F1", // indigo
                "14B8A6", // teal
                "A855F7", // purple
                "EAB308", // yellow
                "F43F5E", // rose
                "22C55E", // green-500
                "0EA5E9"  // sky
            };
            
            var backgroundColor = backgroundColors[colorHash];
            
            // Use UI Avatars API
            return $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(initials)}&background={backgroundColor}&color=fff&size=200&font-size=0.6";
        }

        public async Task<User?> UpdateProfileAsync(int userId, UpdateProfileDto updateProfileDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return null;
            }

            user.Name = updateProfileDto.Name;
            user.Bio = updateProfileDto.Bio;
            
            if (!string.IsNullOrEmpty(updateProfileDto.ProfileImageUrl))
            {
                user.ProfileImageUrl = updateProfileDto.ProfileImageUrl;
            }
            
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<string?> SaveProfileImageAsync(IFormFile image, int userId)
        {
            if (image == null || image.Length == 0)
            {
                return null;
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException("Invalid file type. Only JPG, JPEG, PNG, and GIF files are allowed.");
            }

            // Validate file size (max 5MB)
            if (image.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("File size cannot exceed 5MB.");
            }

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "profile-images");
            Directory.CreateDirectory(uploadsPath);

            // Generate unique filename
            var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // Return relative URL path
            return $"/uploads/profile-images/{fileName}";
        }
    }
}