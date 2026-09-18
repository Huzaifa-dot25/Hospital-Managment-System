using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hospital.Application.DTOs.Auth;
using Hospital.Application.Exceptions;
using Hospital.Application.Services.Interfaces;
using Hospital.Domain.Entities.Identity;
using Hospital.Domain.Repositories;
using Hospital.Infrastructure.Authentication;
using Hospital.Infrastructure.Services;
using Hospital.Application.Validations.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using ValidationException = Hospital.Application.Exceptions.ValidationException;

namespace UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _mockRoleManager;
        private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly IOptions<JwtOptions> _jwtOptions;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _mockUserManager = MockUserManager();
            _mockRoleManager = MockRoleManager();
            _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
            _mockEmailService = new Mock<IEmailService>();

            var jwtOptions = new JwtOptions
            {
                Secret = "SuperSecretKeyForTestingWhichNeedsToBeAtLeast32BytesLong!",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiryMinutes = 60
            };
            _jwtOptions = Options.Create(jwtOptions);

            // We mock the validators to simplify tests that don't need real validation, 
            // but we can override it in specific tests if needed.
            var mockLoginValidator = new Mock<IValidator<LoginDto>>();
            mockLoginValidator
                .Setup(v => v.ValidateAsync(It.IsAny<LoginDto>(), default))
                .ReturnsAsync(new ValidationResult());
            _loginValidator = mockLoginValidator.Object;

            var mockRegisterValidator = new Mock<IValidator<RegisterDto>>();
            mockRegisterValidator
                .Setup(v => v.ValidateAsync(It.IsAny<RegisterDto>(), default))
                .ReturnsAsync(new ValidationResult());
            _registerValidator = mockRegisterValidator.Object;

            _sut = new AuthService(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _jwtOptions,
                _mockRefreshTokenRepository.Object,
                _loginValidator,
                _registerValidator,
                _mockEmailService.Object
            );
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponseDto()
        {
            // ARRANGE
            var loginDto = new LoginDto { Email = "test@test.com", Password = "Password123!" };
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@test.com", IsActive = true, FirstName = "John", LastName = "Doe" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Doctor" });

            // ACT
            var result = await _sut.LoginAsync(loginDto);

            // ASSERT
            result.Should().NotBeNull();
            result.Email.Should().Be("test@test.com");
            result.Token.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            
            _mockRefreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ThrowsBadRequestException()
        {
            // ARRANGE
            var loginDto = new LoginDto { Email = "test@test.com", Password = "WrongPassword!" };
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@test.com", IsActive = true };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password)).ReturnsAsync(false); // Wrong password

            // ACT & ASSERT
            await _sut.Invoking(s => s.LoginAsync(loginDto))
                .Should().ThrowAsync<BadRequestException>()
                .WithMessage("Invalid authentication credentials.");
                
            _mockRefreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WhenUserNotFound_ThrowsBadRequestException()
        {
            // ARRANGE
            var loginDto = new LoginDto { Email = "notfound@test.com", Password = "Password123!" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email)).ReturnsAsync((ApplicationUser?)null);

            // ACT & ASSERT
            await _sut.Invoking(s => s.LoginAsync(loginDto))
                .Should().ThrowAsync<BadRequestException>()
                .WithMessage("Invalid authentication credentials.");
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsAuthResponseDto()
        {
            // ARRANGE
            var registerDto = new RegisterDto { Email = "new@test.com", Password = "Password123!", ConfirmPassword = "Password123!", FirstName = "Jane", LastName = "Doe", Role = "Patient" };
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "new@test.com", FirstName = "Jane", LastName = "Doe" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password)).ReturnsAsync(IdentityResult.Success);
            _mockRoleManager.Setup(x => x.RoleExistsAsync(registerDto.Role)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), registerDto.Role)).ReturnsAsync(IdentityResult.Success);

            // ACT
            var result = await _sut.RegisterAsync(registerDto);

            // ASSERT
            result.Should().NotBeNull();
            result.Email.Should().Be("new@test.com");
            result.Token.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            
            _mockRefreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsBadRequestException()
        {
            // ARRANGE
            var registerDto = new RegisterDto { Email = "existing@test.com", Password = "Password123!", ConfirmPassword = "Password123!" };
            var existingUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "existing@test.com" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync(existingUser);

            // ACT & ASSERT
            await _sut.Invoking(s => s.RegisterAsync(registerDto))
                .Should().ThrowAsync<BadRequestException>()
                .WithMessage("A user with this email already exists.");
                
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ThrowsAsync(new Exception("Should not be called"));
        }

        [Fact]
        public async Task RegisterAsync_WithInvalidRole_ThrowsBadRequestException()
        {
            // ARRANGE
            var registerDto = new RegisterDto { Email = "new@test.com", Password = "Password123!", ConfirmPassword = "Password123!", FirstName = "Jane", LastName = "Doe", Role = "InvalidRole" };
            
            _mockUserManager.Setup(x => x.FindByEmailAsync(registerDto.Email)).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password)).ReturnsAsync(IdentityResult.Success);
            _mockRoleManager.Setup(x => x.RoleExistsAsync(registerDto.Role)).ReturnsAsync(false);

            // ACT & ASSERT
            await _sut.Invoking(s => s.RegisterAsync(registerDto))
                .Should().ThrowAsync<BadRequestException>()
                .WithMessage($"Invalid role specified: {registerDto.Role}");
                
            _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
        {
            // ARRANGE
            var refreshTokenString = "some-random-token";
            var oldJwtToken = "old-jwt-token";
            var userId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, Email = "test@test.com", FirstName = "John", LastName = "Doe" };
            
            var existingToken = new RefreshToken
            {
                Token = refreshTokenString,
                Expires = DateTime.MaxValue,
                Revoked = null,
                User = user
            };
            
            _mockRefreshTokenRepository.Setup(x => x.GetActiveTokenAsync(refreshTokenString)).ReturnsAsync(existingToken);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Doctor" });

            // ACT
            var result = await _sut.RefreshTokenAsync(oldJwtToken, refreshTokenString);

            // ASSERT
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBe(refreshTokenString); // New refresh token should be generated
            
            existingToken.Revoked.Should().NotBeNull(); // Old token should be revoked
            
            _mockRefreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }
        
        [Fact]
        public async Task RefreshTokenAsync_WithInvalidToken_ThrowsBadRequestException()
        {
            // ARRANGE
            var refreshTokenString = "invalid-token";
            var oldJwtToken = "old-jwt-token";
            
            _mockRefreshTokenRepository.Setup(x => x.GetActiveTokenAsync(refreshTokenString)).ReturnsAsync((RefreshToken?)null);

            // ACT & ASSERT
            await _sut.Invoking(s => s.RefreshTokenAsync(oldJwtToken, refreshTokenString))
                .Should().ThrowAsync<BadRequestException>()
                .WithMessage("Refresh token not found.");
        }

        // --- Helpers to mock UserManager and RoleManager ---
        
        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
            return mock;
        }

        private static Mock<RoleManager<IdentityRole<Guid>>> MockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole<Guid>>>();
            var mock = new Mock<RoleManager<IdentityRole<Guid>>>(store.Object, null, null, null, null);
            return mock;
        }
    }
}
