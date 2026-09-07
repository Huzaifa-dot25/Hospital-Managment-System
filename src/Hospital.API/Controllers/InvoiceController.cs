using Hospital.Application.DTOs.Billing;
using Hospital.Application.Services.Interfaces;
using Hospital.Shared.Constants;
using Hospital.Shared.Models;
using Hospital.Shared.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Hospital.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        /// <summary>
        /// Returns a paginated, filterable list of invoices.
        /// Available to administrative, accounting, and cashier staff.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Accountant},{Roles.Cashier}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<InvoiceDto>>>> GetInvoices(
            [FromQuery] InvoiceQueryParams queryParams)
        {
            var result = await _invoiceService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedResponse<InvoiceDto>>.SuccessResult(
                result,
                $"Retrieved {result.Items.Count} of {result.TotalCount} invoices"));
        }

        /// <summary>
        /// Returns a single invoice with line items and payment records by ID.
        /// Available to staff and patient.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Accountant},{Roles.Cashier},{Roles.Patient}")]
        public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoiceById(Guid id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            return Ok(ApiResponse<InvoiceDto>.SuccessResult(
                invoice, "Invoice retrieved successfully"));
        }

        /// <summary>
        /// Creates a new patient invoice with itemized charges.
        /// Cashier, Accountant, and Admin.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Accountant},{Roles.Cashier}")]
        public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice(
            [FromBody] CreateInvoiceDto createDto)
        {
            var created = await _invoiceService.CreateInvoiceAsync(createDto);
            return CreatedAtAction(
                nameof(GetInvoiceById),
                new { id = created.Id },
                ApiResponse<InvoiceDto>.SuccessResult(created, "Invoice created successfully"));
        }

        /// <summary>
        /// Cancels an invoice.
        /// Accountant and Admin only.
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin},{Roles.Accountant}")]
        public async Task<ActionResult<ApiResponse<InvoiceDto>>> CancelInvoice(Guid id)
        {
            var updated = await _invoiceService.CancelInvoiceAsync(id);
            return Ok(ApiResponse<InvoiceDto>.SuccessResult(
                updated, "Invoice cancelled successfully"));
        }
    }
}
