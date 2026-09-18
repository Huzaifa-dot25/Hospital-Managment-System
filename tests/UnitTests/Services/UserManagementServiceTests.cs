using FluentAssertions;
using Hospital.Domain.Entities.Identity;
using Hospital.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.Services
{
    public class UserManagementServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _mockRoleManager;
        private readonly UserManagementService _sut;

        public UserManagementServiceTests()
        {
            _mockUserManager = MockUserManager();
            _mockRoleManager = MockRoleManager();
            _sut = new UserManagementService(_mockUserManager.Object, _mockRoleManager.Object);
        }

        [Fact]
        public async Task GetAllRolesAsync_ReturnsAllRoles()
        {
            // ARRANGE
            var roles = new List<IdentityRole<Guid>>
            {
                new IdentityRole<Guid> { Name = "Admin" },
                new IdentityRole<Guid> { Name = "Doctor" },
                new IdentityRole<Guid> { Name = "Nurse" }
            }.AsQueryable();

            var mockRolesQueryable = roles.BuildMock();
            _mockRoleManager.Setup(x => x.Roles).Returns(mockRolesQueryable.Object);

            // ACT
            var result = await _sut.GetAllRolesAsync();

            // ASSERT
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain("Admin");
            result.Should().Contain("Doctor");
            result.Should().Contain("Nurse");
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
