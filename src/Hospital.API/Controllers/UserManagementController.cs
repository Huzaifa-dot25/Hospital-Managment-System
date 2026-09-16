using Hospital.Application.DTOs.User;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Constants;
using Hospital.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin)]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;

        public UserManagementController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserResponseDto>>>> GetAllUsers()
        {
            var result = await _userManagementService.GetAllUsersAsync();
            return Ok(ApiResponse<IEnumerable<UserResponseDto>>.SuccessResult(result, "Users retrieved successfully"));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetUserById(Guid id)
        {
            var result = await _userManagementService.GetUserByIdAsync(id);
            return Ok(ApiResponse<UserResponseDto>.SuccessResult(result, "User retrieved successfully"));
        }

        [HttpPut("{id}/roles")]
        [Authorize(Roles = Roles.SuperAdmin)] // Only SuperAdmin can change roles
        public async Task<ActionResult<ApiResponse<bool>>> UpdateUserRoles(Guid id, [FromBody] UpdateUserRoleDto dto)
        {
            var result = await _userManagementService.UpdateUserRolesAsync(id, dto);
            return Ok(ApiResponse<bool>.SuccessResult(result, "User roles updated successfully"));
        }

        [HttpPut("{id}/toggle-status")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleUserStatus(Guid id)
        {
            var result = await _userManagementService.ToggleUserStatusAsync(id);
            return Ok(ApiResponse<bool>.SuccessResult(result, "User status toggled successfully"));
        }
    }
}
