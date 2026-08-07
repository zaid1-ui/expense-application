using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ExpenseApp.Data;
using ExpenseApp.DTOs;
using ExpenseApp.Enums;
using ExpenseApp.Models;

namespace ExpenseApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly ExpenseAppDbContext _context;
        private readonly ILogger<ExpenseController> _logger;
        private const decimal MaxExpenseAmount = 5000;

        public ExpenseController(ExpenseAppDbContext context, ILogger<ExpenseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ================= EMPLOYEE =================

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<ActionResult> CreateExpenseForm(ExpenseFormCreateDto dto)
        {
            try
            {
                if (dto.Items == null || dto.Items.Count == 0)
                    return BadRequest(new { message = "Expense form must have at least one expense item." });

                foreach (var item in dto.Items)
                {
                    if (item.Amount > MaxExpenseAmount)
                        return BadRequest(new { message = $"Expense amount cannot exceed {MaxExpenseAmount}." });
                }

                var employeeId = GetUserId();

                var form = new ExpenseForm
                {
                    EmployeeId = employeeId,
                    Currency = dto.Currency,
                    Status = ExpenseStatus.PendingApproval,
                    CreatedDate = DateTime.Now,
                    SubmittedDate = DateTime.Now,
                    ExpenseItems = dto.Items.Select(i => new ExpenseItem
                    {
                        ExpenseDate = i.ExpenseDate,
                        Purpose = i.Purpose,
                        Category = i.Category,
                        Amount = i.Amount
                    }).ToList()
                };

                _context.ExpenseForms.Add(form);
                await _context.SaveChangesAsync();

                _context.ApprovalHistories.Add(new ApprovalHistory
                {
                    ExpenseFormId = form.Id,
                    ActionByUserId = employeeId,
                    Action = "Submitted",
                    ActionDate = DateTime.Now
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation("Expense form {FormId} submitted by user {UserId}", form.Id, employeeId);
                return Ok(new { message = "Expense submitted successfully.", formId = form.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense form");
                return StatusCode(500, new { message = "An error occurred while submitting the expense." });
            }
        }

        [HttpGet("my-forms")]
        [Authorize(Roles = "Employee")]
        public async Task<ActionResult<List<ExpenseFormResponseDto>>> GetMyForms(
            [FromQuery] string? status, [FromQuery] string? currency)
        {
            try
            {
                var employeeId = GetUserId();
                var query = _context.ExpenseForms
                    .Include(f => f.ExpenseItems)
                    .Include(f => f.Employee)
                    .Where(f => f.EmployeeId == employeeId);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(f => f.Status.ToString() == status);

                if (!string.IsNullOrEmpty(currency))
                    query = query.Where(f => f.Currency == currency);

                var forms = await query.OrderByDescending(f => f.CreatedDate).ToListAsync();

                return Ok(forms.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching employee forms");
                return StatusCode(500, new { message = "An error occurred while fetching forms." });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Employee")]
        public async Task<ActionResult> EditExpenseForm(int id, ExpenseFormCreateDto dto)
        {
            try
            {
                var employeeId = GetUserId();
                var form = await _context.ExpenseForms
                    .Include(f => f.ExpenseItems)
                    .FirstOrDefaultAsync(f => f.Id == id && f.EmployeeId == employeeId);

                if (form == null)
                    return NotFound(new { message = "Expense form not found." });

                if (form.Status != ExpenseStatus.PendingApproval && form.Status != ExpenseStatus.ChangeRequested)
                    return BadRequest(new { message = "Only pending or change-requested forms can be edited." });

                foreach (var item in dto.Items)
                {
                    if (item.Amount > MaxExpenseAmount)
                        return BadRequest(new { message = $"Expense amount cannot exceed {MaxExpenseAmount}." });
                }

                _context.ExpenseItems.RemoveRange(form.ExpenseItems);
                form.ExpenseItems = dto.Items.Select(i => new ExpenseItem
                {
                    ExpenseDate = i.ExpenseDate,
                    Purpose = i.Purpose,
                    Category = i.Category,
                    Amount = i.Amount
                }).ToList();

                form.Currency = dto.Currency;
                form.Status = ExpenseStatus.PendingApproval;
                form.RejectionReason = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Expense form {FormId} edited by user {UserId}", id, employeeId);
                return Ok(new { message = "Expense updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing expense form {FormId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the expense." });
            }
        }

        // ================= MANAGER =================

        [HttpGet("awaiting-approval")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<List<ExpenseFormResponseDto>>> GetAwaitingApproval(
            [FromQuery] string? currency, [FromQuery] string? employeeName)
        {
            try
            {
                var managerId = GetUserId();

                var query = _context.ExpenseForms
                    .Include(f => f.ExpenseItems)
                    .Include(f => f.Employee)
                    .Where(f => f.Status == ExpenseStatus.PendingApproval && f.Employee.ManagerId == managerId);

                if (!string.IsNullOrEmpty(currency))
                    query = query.Where(f => f.Currency == currency);

                if (!string.IsNullOrEmpty(employeeName))
                    query = query.Where(f => f.Employee.FullName.Contains(employeeName));

                var forms = await query.OrderByDescending(f => f.CreatedDate).ToListAsync();
                return Ok(forms.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching forms awaiting approval");
                return StatusCode(500, new { message = "An error occurred while fetching forms." });
            }
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> ApproveForm(int id)
        {
            try
            {
                var managerId = GetUserId();
                var form = await _context.ExpenseForms
                    .Include(f => f.Employee)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (form == null) return NotFound(new { message = "Form not found." });
                if (form.Employee.ManagerId != managerId) return Forbid();
                if (form.Status != ExpenseStatus.PendingApproval)
                    return BadRequest(new { message = "Only pending forms can be approved." });

                form.Status = ExpenseStatus.Approved;
                _context.ApprovalHistories.Add(new ApprovalHistory
                {
                    ExpenseFormId = form.Id,
                    ActionByUserId = managerId,
                    Action = "Approved",
                    ActionDate = DateTime.Now
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Expense form {FormId} approved by manager {ManagerId}", id, managerId);
                return Ok(new { message = "Expense form approved." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving expense form {FormId}", id);
                return StatusCode(500, new { message = "An error occurred while approving the form." });
            }
        }

        [HttpPost("{id}/request-change")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> RequestChange(int id, RejectDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Reason))
                    return BadRequest(new { message = "Reason is required to request a change." });

                var managerId = GetUserId();
                var form = await _context.ExpenseForms
                    .Include(f => f.Employee)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (form == null) return NotFound(new { message = "Form not found." });
                if (form.Employee.ManagerId != managerId) return Forbid();
                if (form.Status != ExpenseStatus.PendingApproval)
                    return BadRequest(new { message = "Only pending forms can have changes requested." });

                form.Status = ExpenseStatus.ChangeRequested;
                form.RejectionReason = dto.Reason;

                _context.ApprovalHistories.Add(new ApprovalHistory
                {
                    ExpenseFormId = form.Id,
                    ActionByUserId = managerId,
                    Action = "ChangeRequested",
                    Reason = dto.Reason,
                    ActionDate = DateTime.Now
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Change requested for form {FormId} by manager {ManagerId}", id, managerId);
                return Ok(new { message = "Change requested. Employee has been notified." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting change for form {FormId}", id);
                return StatusCode(500, new { message = "An error occurred while requesting change." });
            }
        }

        // ================= ACCOUNTANT =================

        [HttpGet("to-be-paid")]
        [Authorize(Roles = "Accountant")]
        public async Task<ActionResult<List<ExpenseFormResponseDto>>> GetToBePaid(
            [FromQuery] string? currency, [FromQuery] string? employeeName)
        {
            try
            {
                var query = _context.ExpenseForms
                    .Include(f => f.ExpenseItems)
                    .Include(f => f.Employee)
                    .Where(f => f.Status == ExpenseStatus.Approved);

                if (!string.IsNullOrEmpty(currency))
                    query = query.Where(f => f.Currency == currency);

                if (!string.IsNullOrEmpty(employeeName))
                    query = query.Where(f => f.Employee.FullName.Contains(employeeName));

                var forms = await query.OrderByDescending(f => f.CreatedDate).ToListAsync();
                return Ok(forms.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching forms to be paid");
                return StatusCode(500, new { message = "An error occurred while fetching forms." });
            }
        }

        [HttpPost("{id}/pay")]
        [Authorize(Roles = "Accountant")]
        public async Task<ActionResult> PayForm(int id)
        {
            try
            {
                var accountantId = GetUserId();
                var form = await _context.ExpenseForms.FirstOrDefaultAsync(f => f.Id == id);

                if (form == null) return NotFound(new { message = "Form not found." });
                if (form.Status != ExpenseStatus.Approved)
                    return BadRequest(new { message = "Only approved forms can be paid." });

                form.Status = ExpenseStatus.Paid;

                _context.ApprovalHistories.Add(new ApprovalHistory
                {
                    ExpenseFormId = form.Id,
                    ActionByUserId = accountantId,
                    Action = "Paid",
                    ActionDate = DateTime.Now
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Expense form {FormId} paid by accountant {AccountantId}", id, accountantId);
                return Ok(new { message = "Expense form marked as paid." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error paying expense form {FormId}", id);
                return StatusCode(500, new { message = "An error occurred while processing payment." });
            }
        }

        // ================= SHARED HELPER =================

        private static ExpenseFormResponseDto MapToDto(ExpenseForm f) => new()
        {
            Id = f.Id,
            EmployeeName = f.Employee?.FullName ?? "",
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
        };
    }

    public class RejectDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}