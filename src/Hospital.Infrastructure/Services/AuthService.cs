using Hospital.Application.DTOs.Auth;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities.Identity;
using Hospital.Domain.Repositories;
using Hospital.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
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

namespace Hospital.Infrastructure.Services
{
    /// <summary>
    /// Handles all authentication operations: Login, Register, Refresh Token.
    /// 
    /// Notice: This class NO LONGER depends on ApplicationDbContext.
    /// That was an architectural violation — Infrastructure should not reference Persistence.
    /// 
    /// Instead, we depend on IRefreshTokenRepository (an interface in Domain).
    /// The actual implementation lives in Persistence. This is Clean Architecture in action:
    /// 
    ///   Infrastructure (AuthService) → Domain (IRefreshTokenRepository) ← Persistence (RefreshTokenRepository)
    /// 
    /// Infrastructure and Persistence both point inward to Domain. They never point at each other.
    /// This means we can swap out the database entirely without changing AuthService.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly JwtOptions _jwtOptions;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IOptions<JwtOptions> jwtOptions,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtOptions = jwtOptions.Value;
            _refreshTokenRepository = refreshTokenRepository;
        }

        /// <summary>
        /// Validates credentials, generates JWT + refresh token, stores refresh token.
        /// </summary>
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Step 1: Find user by email
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null || !user.IsActive)
            {
                // Security note: We say "Invalid credentials" for BOTH wrong email AND wrong password.
                // Never say "Email not found" — that leaks information about registered emails.
                throw new Exception("Invalid authentication credentials.");
            }

            // Step 2: Verify password using ASP.NET Identity's secure password checker
            // (It handles hashing, salting — never compare passwords directly)
            var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!passwordValid)
            {
                throw new Exception("Invalid authentication credentials.");
            }

            // Step 3: Get roles for this user — we embed roles in the JWT as claims
            var userRoles = await _userManager.GetRolesAsync(user);

            // Step 4: Generate a short-lived JWT (expires in 30 minutes by default)
            var token = GenerateJwtToken(user, userRoles);

            // Step 5: Generate a long-lived refresh token (random 32-byte string, 7 days)
            var refreshToken = CreateRefreshToken(user.Id);

            // Step 6: Save the refresh token to the database
            await _refreshTokenRepository.AddAsync(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            return MapToAuthResponse(user, token, refreshToken);
        }

        /// <summary>
        /// Creates a new user account and assigns the requested role.
        /// </summary>
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check for duplicate email
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new Exception("A user with this email already exists.");
            }

            if (registerDto.Password != registerDto.ConfirmPassword)
            {
                throw new Exception("Passwords do not match.");
            }

            // Create the user — UserName = Email is a common convention
            var user = new ApplicationUser
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                UserName = registerDto.Email
            };

            // UserManager handles hashing the password before saving to DB
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Registration failed: {errors}");
            }

            // Create the role if it doesn't exist yet, then assign it to the user
            if (!await _roleManager.RoleExistsAsync(registerDto.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(registerDto.Role));
            }
            await _userManager.AddToRoleAsync(user, registerDto.Role);

            // Generate tokens and log the user in immediately after registration
            var userRoles = new List<string> { registerDto.Role };
            var token = GenerateJwtToken(user, userRoles);
            var refreshToken = CreateRefreshToken(user.Id);

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            return MapToAuthResponse(user, token, refreshToken);
        }

        /// <summary>
        /// Validates a refresh token, revokes it, and issues a new JWT + refresh token pair.
        /// 
        /// Token rotation: every time you refresh, the old refresh token is revoked and
        /// a new one is issued. This limits the damage if a refresh token is stolen.
        /// </summary>
        public async Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken)
        {
            // Find the refresh token in the database (including the user)
            var existingToken = await _refreshTokenRepository.GetActiveTokenAsync(refreshToken);

            if (existingToken == null)
            {
                throw new Exception("Refresh token not found.");
            }

            // Check if it's still active (not revoked + not expired)
            if (!existingToken.IsActive)
            {
                throw new Exception("Refresh token is no longer active. Please login again.");
            }

            var user = existingToken.User;
            var userRoles = await _userManager.GetRolesAsync(user);

            // Revoke the old refresh token
            existingToken.Revoked = DateTime.UtcNow;
            existingToken.ReplacedByToken = "pending"; // Will be updated below

            // Issue a new JWT and a new refresh token (token rotation)
            var newJwtToken = GenerateJwtToken(user, userRoles);
            var newRefreshToken = CreateRefreshToken(user.Id);

            // Link the old token to the new one for audit trail
            existingToken.ReplacedByToken = newRefreshToken.Token;

            await _refreshTokenRepository.AddAsync(newRefreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            return MapToAuthResponse(user, newJwtToken, newRefreshToken);
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a signed JWT token containing user identity claims and roles.
        /// 
        /// A JWT has 3 parts separated by dots: Header.Payload.Signature
        /// - Header: algorithm (HS256) and type (JWT)
        /// - Payload: claims (user id, email, roles, expiry)
        /// - Signature: HMAC-SHA256 of header+payload using the secret key
        /// 
        /// Anyone can read the payload (it's base64 encoded, not encrypted).
        /// But no one can MODIFY it without invalidating the signature.
        /// </summary>
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                // Standard JWT claims
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),   // Subject = user id
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token id
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                // Custom claim for easy access in CurrentUserService
                new Claim("uid", user.Id.ToString()),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName)
            };

            // Add one claim per role (e.g., ClaimTypes.Role = "Admin")
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

            var jwtToken = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

        /// <summary>
        /// Creates a cryptographically secure random refresh token.
        /// 
        /// RandomNumberGenerator is cryptographically secure — much better than Random.
        /// 32 bytes = 256 bits of entropy → practically impossible to brute force.
        /// </summary>
        private RefreshToken CreateRefreshToken(Guid userId)
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                UserId = userId
            };
        }

        /// <summary>
        /// Maps user + token data into the AuthResponseDto that gets returned to the client.
        /// </summary>
        private static AuthResponseDto MapToAuthResponse(ApplicationUser user, string token, RefreshToken refreshToken)
        {
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
    }
}
