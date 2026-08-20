using Hospital.Application.DTOs.Auth;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities.Identity;
using Hospital.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Hospital.Persistence.Contexts;

namespace Hospital.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly JwtOptions _jwtOptions;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IOptions<JwtOptions> jwtOptions,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtOptions = jwtOptions.Value;
            _context = context;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                throw new Exception("Invalid authentication credentials.");
            }

            var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!result)
            {
                throw new Exception("Invalid authentication credentials.");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, userRoles);
            var refreshToken = GenerateRefreshToken();

            // Save refresh token
            user.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken.Token,
                Expires = refreshToken.Expires,
                Created = refreshToken.Created,
                UserId = user.Id
            });
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Token = token,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.Expires
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new Exception("User already exists.");
            }

            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                throw new Exception("Passwords do not match.");
            }

            var user = new ApplicationUser
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                UserName = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create user: {errors}");
            }

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(registerDto.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(registerDto.Role));
            }

            await _userManager.AddToRoleAsync(user, registerDto.Role);

            var userRoles = new List<string> { registerDto.Role };
            var token = GenerateJwtToken(user, userRoles);
            var refreshToken = GenerateRefreshToken();

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken.Token,
                Expires = refreshToken.Expires,
                Created = refreshToken.Created,
                UserId = user.Id
            });
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Token = token,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.Expires
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken)
        {
            var user = await _context.Users.Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));

            if (user == null)
            {
                throw new Exception("Invalid refresh token.");
            }

            var existingToken = user.RefreshTokens.Single(t => t.Token == refreshToken);
            if (!existingToken.IsActive)
            {
                throw new Exception("Refresh token is inactive or expired.");
            }

            // Revoke current token
            existingToken.Revoked = DateTime.UtcNow;
            
            var userRoles = await _userManager.GetRolesAsync(user);
            var newJwtToken = GenerateJwtToken(user, userRoles);
            var newRefreshToken = GenerateRefreshToken();

            existingToken.ReplacedByToken = newRefreshToken.Token;

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken.Token,
                Expires = newRefreshToken.Expires,
                Created = newRefreshToken.Created,
                UserId = user.Id
            });

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Token = newJwtToken,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiration = newRefreshToken.Expires
            };
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("uid", user.Id.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

            var token = new JwtSecurityToken(
                _jwtOptions.Issuer,
                _jwtOptions.Audience,
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private (string Token, DateTime Expires, DateTime Created) GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return (
                Convert.ToBase64String(randomNumber), 
                DateTime.UtcNow.AddDays(7), // Expiry
                DateTime.UtcNow // Created
            );
        }
    }
}
