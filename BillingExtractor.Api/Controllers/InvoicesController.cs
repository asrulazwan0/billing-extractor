using MediatR;
using Microsoft.AspNetCore.Mvc;
using BillingExtractor.Application.Commands;
using BillingExtractor.Application.DTOs;
using BillingExtractor.Application.Queries;

namespace BillingExtractor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IMediator mediator, ILogger<InvoicesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Upload and process invoice files
    /// </summary>
    /// <param name="files">Invoice files (PDF, JPG, PNG)</param>
    /// <param name="validate">Enable validation (default: true)</param>
    /// <param name="checkDuplicates">Enable duplicate detection (default: true)</param>
    /// <returns>Processing results</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ProcessInvoicesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<IActionResult> UploadInvoices(
        [FromForm] IFormFile[] files,
        [FromQuery] bool validate = true,
        [FromQuery] bool checkDuplicates = true)
    {
        try
        {
            _logger.LogInformation("Uploading {FileCount} files for processing", files.Length);

            var command = new ProcessInvoicesCommand(files, validate, checkDuplicates);
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice upload");
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Processing Error");
        }
    }

    /// <summary>
    /// Get all processed invoices with optional filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="vendorName">Filter by vendor name</param>
    /// <param name="fromDate">Filter by invoice date from</param>
    /// <param name="toDate">Filter by invoice date to</param>
    /// <returns>List of invoice summaries</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<InvoiceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? vendorName = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var query = new GetAllInvoicesQuery(page, pageSize, vendorName, fromDate, toDate);
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices");
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Query Error");
        }
    }

    /// <summary>
    /// Get invoice by ID
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>Invoice details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceById(Guid id)
    {
        try
        {
            var query = new GetInvoiceByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(new { message = $"Invoice with ID {id} not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice by ID: {InvoiceId}", id);
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Query Error");
        }
    }

    /// <summary>
    /// Delete an invoice
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInvoice(Guid id)
    {
        // Note: In a real application, you would have a DeleteInvoiceCommand
        // For now, we'll implement a simple version
        try
        {
            // Implementation would go here
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice: {InvoiceId}", id);
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Deletion Error");
        }
    }
}