using Hospital.Application.DTOs.Billing;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Constants;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of payments.
        /// Available to administrative, accounting, and cashier staff.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Accountant},{Roles.Cashier}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PaymentDto>>>> GetPayments(
            [FromQuery] PaymentQueryParams queryParams)
        {
            var result = await _paymentService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<PaymentDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} payments"));
        }

        /// <summary>
        /// Returns a single payment receipt by ID.
        /// Available to staff and patient.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Accountant},{Roles.Cashier},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<PaymentDto>>> GetPaymentById(Guid id)
        {
            var payment = await _paymentService.GetByIdAsync(id);
            return Ok(ApiResponse<PaymentDto>.SuccessResult(
                payment, "Payment receipt retrieved successfully"));
        }

        /// <summary>
        /// Returns all payments made towards a specific invoice.
        /// Available to staff and patient.
        /// </summary>
        [HttpGet("invoice/{invoiceId:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Accountant},{Roles.Cashier},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<PaymentDto>>>> GetPaymentsByInvoiceId(Guid invoiceId)
        {
            var payments = await _paymentService.GetByInvoiceIdAsync(invoiceId);
            return Ok(ApiResponse<IReadOnlyList<PaymentDto>>.SuccessResult(
                payments, $"Retrieved {payments.Count} payments for invoice"));
        }

        /// <summary>
        /// Processes a payment against an invoice.
        /// Cashier, Accountant, and Admin only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Accountant},{Roles.Cashier}")]
        public async Task<ActionResult<ApiResponse<PaymentDto>>> ProcessPayment(
            [FromBody] ProcessPaymentDto paymentDto)
        {
            Guid? cashierUserId = null;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdClaim, out var parsedId))
                cashierUserId = parsedId;

            var created = await _paymentService.ProcessPaymentAsync(paymentDto, cashierUserId);
            return CreatedAtAction(
                nameof(GetPaymentById),
                new { id = created.Id },
                ApiResponse<PaymentDto>.SuccessResult(created, "Payment processed successfully"));
        }
    }
}
