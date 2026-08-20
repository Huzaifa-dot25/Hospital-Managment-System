using Hospital.Application.DTOs.Auth;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken);
    }
}
