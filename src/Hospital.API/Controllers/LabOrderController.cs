using Hospital.Application.DTOs.Laboratory;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Constants;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class LabOrderController : ControllerBase
    {
        private readonly ILabOrderService _labOrderService;

        public LabOrderController(ILabOrderService labOrderService)
        {
            _labOrderService = labOrderService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of laboratory orders.
        /// Available to clinical and laboratory staff.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.LabTechnician}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<LabOrderDto>>>> GetLabOrders(
            [FromQuery] LabOrderQueryParams queryParams)
        {
            var result = await _labOrderService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<LabOrderDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} lab orders"));
        }

        /// <summary>
        /// Returns a single lab order with line items and test details by ID.
        /// Available to staff and patient.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor},{Roles.Nurse},{Roles.LabTechnician},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<LabOrderDto>>> GetLabOrderById(Guid id)
        {
            var order = await _labOrderService.GetByIdAsync(id);
            return Ok(ApiResponse<LabOrderDto>.SuccessResult(
                order, "Lab order retrieved successfully"));
        }

        /// <summary>
        /// Creates a new laboratory order for a patient.
        /// Doctor and Admin only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
        public async Task<ActionResult<ApiResponse<LabOrderDto>>> CreateLabOrder(
            [FromBody] CreateLabOrderDto createDto)
        {
            var created = await _labOrderService.CreateOrderAsync(createDto);
            return CreatedAtAction(
                nameof(GetLabOrderById),
                new { id = created.Id },
                ApiResponse<LabOrderDto>.SuccessResult(created, "Lab order created successfully"));
        }

        /// <summary>
        /// Marks sample/specimen collected for a laboratory order.
        /// Nurse, LabTechnician, and Admin.
        /// </summary>
        [HttpPost("{id:guid}/collect-sample")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Nurse},{Roles.LabTechnician}")]
        public async Task<ActionResult<ApiResponse<LabOrderDto>>> CollectSample(
            Guid id, [FromBody] CollectSampleDto collectDto)
        {
            var updated = await _labOrderService.CollectSampleAsync(id, collectDto);
            return Ok(ApiResponse<LabOrderDto>.SuccessResult(
                updated, "Sample collected successfully"));
        }

        /// <summary>
        /// Records test results for a laboratory order.
        /// LabTechnician and Admin only.
        /// </summary>
        [HttpPost("{id:guid}/results")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.LabTechnician}")]
        public async Task<ActionResult<ApiResponse<LabOrderDto>>> RecordResults(
            Guid id, [FromBody] RecordLabResultsDto resultsDto)
        {
            Guid? labTechId = null;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdClaim, out var parsedId))
                labTechId = parsedId;

            var updated = await _labOrderService.RecordResultsAsync(id, resultsDto, labTechId);
            return Ok(ApiResponse<LabOrderDto>.SuccessResult(
                updated, "Lab test results recorded successfully"));
        }

        /// <summary>
        /// Cancels a laboratory order.
        /// Doctor and Admin only.
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Doctor}")]
        public async Task<ActionResult<ApiResponse<LabOrderDto>>> CancelLabOrder(Guid id)
        {
            var updated = await _labOrderService.CancelOrderAsync(id);
            return Ok(ApiResponse<LabOrderDto>.SuccessResult(
                updated, "Lab order cancelled successfully"));
        }
    }
}
