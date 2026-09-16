using Hospital.Application.DTOs.Auth;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    /// <summary>
    /// Handles authentication: register, login, and refresh token.
    /// 
    /// Key security concept:
    /// - Register and Login endpoints are [AllowAnonymous] — they MUST be public
    ///   because users haven't logged in yet so they have no token.
    /// - RefreshToken is also [AllowAnonymous] because when calling it, the JWT has expired
    ///   (that's WHY you're refreshing it). The security is the refresh token itself.
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT access token + refresh token.
        /// </summary>
        /// <remarks>
        /// The JWT token expires in 30 minutes (configured in appsettings.json).
        /// The refresh token is valid for 7 days.
        /// Store the refresh token securely — it's like a long-lived password.
        /// </remarks>
        [HttpPost("login")]
        [AllowAnonymous] // ← Explicitly marks this as public. No token required.
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResult(result, "Login successful"));
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <remarks>
        /// In a real hospital system, registration is typically restricted:
        /// patients are registered by receptionists, doctors by admins.
        /// Self-registration would be admin-configurable.
        /// For now, we keep it open for development purposes.
        /// </remarks>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResult(result, "Registration successful"));
        }

        /// <summary>
        /// Uses a valid refresh token to generate a new JWT access token.
        /// Call this when you receive a 401 Unauthorized response from any protected endpoint.
        /// </summary>
        /// <remarks>
        /// Flow:
        ///   1. User's JWT expires → gets 401 from API
        ///   2. Frontend calls POST /api/v1/auth/refresh-token with old JWT + refresh token
        ///   3. This endpoint validates the refresh token, revokes it, issues a new pair
        ///   4. Frontend stores the new tokens and retries the original request
        /// </remarks>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request.Token, request.RefreshToken);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResult(result, "Token refreshed successfully"));
        }
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            // Extract the user ID from the JWT claims. We use the "uid" claim we set during login.
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
            return Ok(ApiResponse<bool>.SuccessResult(result, "Password changed successfully"));
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
            return Ok(ApiResponse<bool>.SuccessResult(result, "If the email exists, a password reset link has been sent."));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var result = await _authService.ResetPasswordAsync(resetPasswordDto);
            return Ok(ApiResponse<bool>.SuccessResult(result, "Password reset successfully"));
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ConfirmEmail([FromBody] ConfirmEmailDto confirmEmailDto)
        {
            var result = await _authService.ConfirmEmailAsync(confirmEmailDto);
            return Ok(ApiResponse<bool>.SuccessResult(result, "Email confirmed successfully"));
        }
    }

    /// <summary>
    /// Request body for the refresh-token endpoint.
    /// Both the expired JWT and the refresh token are required.
    /// </summary>
    public class RefreshTokenRequestDto
    {
        /// <summary>The expired (or about to expire) JWT access token.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>The refresh token received during login.</summary>
        public string RefreshToken { get; set; } = string.Empty;
    }
}
