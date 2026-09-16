using Hospital.Application.DTOs.User;
using Hospital.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto> GetUserByIdAsync(Guid userId);
        Task<bool> UpdateUserRolesAsync(Guid userId, UpdateUserRoleDto updateUserRoleDto);
        Task<bool> ToggleUserStatusAsync(Guid userId);
    }
}
