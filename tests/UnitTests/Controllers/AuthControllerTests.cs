using FluentAssertions;
using Hospital.API.Controllers;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _sut;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _sut = new AuthController(_mockAuthService.Object);
        }

        [Fact]
        public async Task Login_WithValidDto_ReturnsOkResultWithAuthResponse()
        {
            // ARRANGE
            var loginDto = new LoginDto { Email = "test@test.com", Password = "Password123!" };
            var authResponse = new AuthResponseDto 
            { 
                UserId = Guid.NewGuid(), 
                Email = "test@test.com", 
                Token = "jwt-token", 
                RefreshToken = "refresh-token" 
            };
            
            _mockAuthService.Setup(s => s.LoginAsync(loginDto)).ReturnsAsync(authResponse);

            // ACT
            var result = await _sut.Login(loginDto);

            // ASSERT
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AuthResponseDto>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeEquivalentTo(authResponse);
            apiResponse.Message.Should().Be("Login successful");
        }

        [Fact]
        public async Task Register_WithValidDto_ReturnsOkResultWithAuthResponse()
        {
            // ARRANGE
            var registerDto = new RegisterDto { Email = "new@test.com", Password = "Password123!", FirstName = "John", LastName = "Doe" };
            var authResponse = new AuthResponseDto 
            { 
                UserId = Guid.NewGuid(), 
                Email = "new@test.com", 
                Token = "jwt-token", 
                RefreshToken = "refresh-token" 
            };
            
            _mockAuthService.Setup(s => s.RegisterAsync(registerDto)).ReturnsAsync(authResponse);

            // ACT
            var result = await _sut.Register(registerDto);

            // ASSERT
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AuthResponseDto>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeEquivalentTo(authResponse);
            apiResponse.Message.Should().Be("Registration successful");
        }

        [Fact]
        public async Task RefreshToken_WithValidDto_ReturnsOkResultWithNewTokens()
        {
            // ARRANGE
            var requestDto = new RefreshTokenRequestDto { Token = "old-jwt", RefreshToken = "old-refresh" };
            var authResponse = new AuthResponseDto 
            { 
                UserId = Guid.NewGuid(), 
                Email = "test@test.com", 
                Token = "new-jwt-token", 
                RefreshToken = "new-refresh-token" 
            };
            
            _mockAuthService.Setup(s => s.RefreshTokenAsync(requestDto.Token, requestDto.RefreshToken)).ReturnsAsync(authResponse);

            // ACT
            var result = await _sut.RefreshToken(requestDto);

            // ASSERT
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AuthResponseDto>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeEquivalentTo(authResponse);
            apiResponse.Message.Should().Be("Token refreshed successfully");
        }
    }
}
