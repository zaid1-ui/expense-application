using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseApp.Data;
using ExpenseApp.DTOs;
using ExpenseApp.Enums;

namespace ExpenseApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ExpenseAppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ExpenseAppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("transactions")]
        public async Task<ActionResult> GetAllTransactions(
            [FromQuery] string? status, [FromQuery] string? employeeName)
        {
            try
            {
                var query = _context.ExpenseForms
                    .Include(f => f.ExpenseItems)
                    .Include(f => f.Employee)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(f => f.Status.ToString() == status);

                if (!string.IsNullOrEmpty(employeeName))
                    query = query.Where(f => f.Employee.FullName.Contains(employeeName));

                var forms = await query.OrderByDescending(f => f.CreatedDate).ToListAsync();

                var result = forms.Select(f => new ExpenseFormResponseDto
                {
                    Id = f.Id,
                    EmployeeName = f.Employee.FullName,
                    Currency = f.Currency,
                    Status = f.Status.ToString(),
                    TotalAmount = f.ExpenseItems.Sum(i => i.Amount),
                    CreatedDate = f.CreatedDate,
                    RejectionReason = f.RejectionReason,
                    Items = f.ExpenseItems.Select(i => new ExpenseItemDto
                    {
                        ExpenseDate = i.ExpenseDate,
                        Purpose = i.Purpose,
                        Category = i.Category,
                        Amount = i.Amount
                    }).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all transactions");
                return StatusCode(500, new { message = "An error occurred while fetching transactions." });
            }
        }

        [HttpGet("approval-history")]
        public async Task<ActionResult> GetApprovalHistory(
            [FromQuery] string? action, [FromQuery] string? employeeName)
        {
            try
            {
                var query = _context.ApprovalHistories
                    .Include(h => h.ExpenseForm)
                    .Include(h => h.ActionByUser)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(h => h.Action == action);

                if (!string.IsNullOrEmpty(employeeName))
                    query = query.Where(h => h.ActionByUser.FullName.Contains(employeeName));

                var history = await query
                    .OrderByDescending(h => h.ActionDate)
                    .Select(h => new
                    {
                        h.Id,
                        ExpenseFormId = h.ExpenseFormId,
                        ActionBy = h.ActionByUser.FullName,
                        ActionByRole = h.ActionByUser.Role.ToString(),
                        h.Action,
                        h.Reason,
                        h.ActionDate
                    })
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching approval history");
                return StatusCode(500, new { message = "An error occurred while fetching history." });
            }
        }

        [HttpGet("reports/by-status")]
        public async Task<ActionResult> GetReportByStatus()
        {
            try
            {
                var report = await _context.ExpenseForms
                    .Include(f => f.ExpenseItems)
                    .GroupBy(f => f.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        FormCount = g.Count(),
                        TotalAmount = g.SelectMany(f => f.ExpenseItems).Sum(i => i.Amount)
                    })
                    .ToListAsync();

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating status report");
                return StatusCode(500, new { message = "An error occurred while generating the report." });
            }
        }

        [HttpGet("reports/by-employee")]
        public async Task<ActionResult> GetReportByEmployee()
        {
            try
            {
                var report = await _context.ExpenseForms
                    .Include(f => f.Employee)
                    .Include(f => f.ExpenseItems)
                    .GroupBy(f => f.Employee.FullName)
                    .Select(g => new
                    {
                        EmployeeName = g.Key,
                        FormCount = g.Count(),
                        TotalAmount = g.SelectMany(f => f.ExpenseItems).Sum(i => i.Amount)
                    })
                    .ToListAsync();

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating employee report");
                return StatusCode(500, new { message = "An error occurred while generating the report." });
            }
        }

        [HttpGet("reports/by-category")]
        public async Task<ActionResult> GetReportByCategory()
        {
            try
            {
                var report = await _context.ExpenseItems
                    .Include(i => i.ExpenseForm)
                    .GroupBy(i => i.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        ItemCount = g.Count(),
                        TotalAmount = g.Sum(i => i.Amount)
                    })
                    .ToListAsync();

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating category report");
                return StatusCode(500, new { message = "An error occurred while generating the report." });
            }
        }

        [HttpGet("reports/monthly")]
        public async Task<ActionResult> GetMonthlyReport()
        {
            try
            {
                var forms = await _context.ExpenseForms
                    .Include(f => f.ExpenseItems)
                    .ToListAsync();

                var report = forms
                    .GroupBy(f => new { f.CreatedDate.Year, f.CreatedDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        FormCount = g.Count(),
                        TotalAmount = g.SelectMany(f => f.ExpenseItems).Sum(i => i.Amount)
                    })
                    .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
                    .ToList();

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly report");
                return StatusCode(500, new { message = "An error occurred while generating the report." });
            }
        }
    }
}