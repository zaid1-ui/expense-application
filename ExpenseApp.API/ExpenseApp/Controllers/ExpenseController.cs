using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpenseApp.Data.Rows;
using ExpenseApp.DTOs;
using ExpenseApp.Enums;
using ExpenseApp.Services;

namespace ExpenseApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly DapperContext _dapper;
        private readonly ILogger<ExpenseController> _logger;
        private const decimal MaxExpenseAmount = 5000;
        private static readonly string[] AllowedCurrencies = { "PKR", "USD", "EUR", "TL", "INR" };

        public ExpenseController(DapperContext dapper, ILogger<ExpenseController> logger)
        {
            _dapper = dapper;
            _logger = logger;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Single source of truth for what makes a submitted/edited form valid —
        // the frontend mirrors these rules for UX, but this is what actually
        // decides what lands in the database.
        private static string? ValidateExpenseForm(ExpenseFormCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Currency) || !AllowedCurrencies.Contains(dto.Currency))
                return $"Currency must be one of: {string.Join(", ", AllowedCurrencies)}.";

            if (dto.Items == null || dto.Items.Count == 0)
                return "Expense form must have at least one expense item.";

            foreach (var item in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Purpose))
                    return "Purpose is required for every expense item.";

                if (item.Purpose.Trim().Length > 200)
                    return "Purpose must be 200 characters or fewer.";

                if (string.IsNullOrWhiteSpace(item.Category))
                    return "Category is required for every expense item.";

                if (item.ExpenseDate == default)
                    return "Expense date is required for every expense item.";

                if (item.ExpenseDate.Date > DateTime.Now.Date)
                    return "Expense date cannot be in the future.";

                if (item.Amount <= 0)
                    return "Expense amount must be greater than 0.";

                if (item.Amount > MaxExpenseAmount)
                    return $"Expense amount cannot exceed {MaxExpenseAmount}.";
            }

            return null;
        }

        // ================= EMPLOYEE =================

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<ActionResult> CreateExpenseForm(ExpenseFormCreateDto dto)
        {
            try
            {
                var validationError = ValidateExpenseForm(dto);
                if (validationError != null)
                    return BadRequest(new { message = validationError });

                var employeeId = GetUserId();
                var now = DateTime.Now;

                using var conn = _dapper.CreateConnection();
                conn.Open();
                using var transaction = conn.BeginTransaction();
                try
                {
                    var formId = await conn.ExecuteScalarAsync<int>(
                        "sp_InsertExpenseForm",
                        new { EmployeeId = employeeId, dto.Currency, Status = (int)ExpenseStatus.PendingApproval, CreatedDate = now, SubmittedDate = now },
                        transaction,
                        commandType: CommandType.StoredProcedure);

                    foreach (var item in dto.Items)
                    {
                        await conn.ExecuteAsync(
                            "sp_InsertExpenseItem",
                            new { ExpenseFormId = formId, item.ExpenseDate, Purpose = item.Purpose.Trim(), item.Category, item.Amount },
                            transaction,
                            commandType: CommandType.StoredProcedure);
                    }

                    await conn.ExecuteAsync(
                        "sp_InsertApprovalHistory",
                        new { ExpenseFormId = formId, ActionByUserId = employeeId, Action = "Submitted", Reason = (string?)null, ActionDate = now },
                        transaction,
                        commandType: CommandType.StoredProcedure);

                    transaction.Commit();

                    _logger.LogInformation("Expense form {FormId} submitted by user {UserId}", formId, employeeId);
                    return Ok(new { message = "Expense submitted successfully.", formId });
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
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
                int? statusInt = Enum.TryParse<ExpenseStatus>(status, out var parsedStatus) ? (int)parsedStatus : null;

                using var conn = _dapper.CreateConnection();
                using var multi = await conn.QueryMultipleAsync(
                    "sp_GetMyForms",
                    new { EmployeeId = employeeId, Status = statusInt, Currency = currency },
                    commandType: CommandType.StoredProcedure);

                var forms = (await multi.ReadAsync<FormRow>()).ToList();
                var items = (await multi.ReadAsync<ItemRow>()).ToList();

                return Ok(ExpenseMapper.ToDtoList(forms, items));
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

                using var conn = _dapper.CreateConnection();
                var form = await conn.QueryFirstOrDefaultAsync<FormRow>(
                    "sp_GetExpenseFormForEdit",
                    new { FormId = id, EmployeeId = employeeId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound(new { message = "Expense form not found." });

                var currentStatus = (ExpenseStatus)form.Status;
                if (currentStatus != ExpenseStatus.PendingApproval && currentStatus != ExpenseStatus.ChangeRequested)
                    return BadRequest(new { message = "Only pending or change-requested forms can be edited." });

                var validationError = ValidateExpenseForm(dto);
                if (validationError != null)
                    return BadRequest(new { message = validationError });

                conn.Open();
                using var transaction = conn.BeginTransaction();
                try
                {
                    await conn.ExecuteAsync(
                        "sp_DeleteExpenseItemsByForm", new { FormId = id }, transaction, commandType: CommandType.StoredProcedure);

                    foreach (var item in dto.Items)
                    {
                        await conn.ExecuteAsync(
                            "sp_InsertExpenseItem",
                            new { ExpenseFormId = id, item.ExpenseDate, Purpose = item.Purpose.Trim(), item.Category, item.Amount },
                            transaction,
                            commandType: CommandType.StoredProcedure);
                    }

                    await conn.ExecuteAsync(
                        "sp_UpdateExpenseForm",
                        new { FormId = id, dto.Currency, Status = (int)ExpenseStatus.PendingApproval, RejectionReason = (string?)null },
                        transaction,
                        commandType: CommandType.StoredProcedure);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

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

                using var conn = _dapper.CreateConnection();
                using var multi = await conn.QueryMultipleAsync(
                    "sp_GetAwaitingApproval",
                    new { ManagerId = managerId, Currency = currency, EmployeeName = employeeName },
                    commandType: CommandType.StoredProcedure);

                var forms = (await multi.ReadAsync<FormRow>()).ToList();
                var items = (await multi.ReadAsync<ItemRow>()).ToList();

                return Ok(ExpenseMapper.ToDtoList(forms, items));
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

                using var conn = _dapper.CreateConnection();
                var form = await conn.QueryFirstOrDefaultAsync<FormDetailRow>(
                    "sp_GetExpenseFormById", new { FormId = id }, commandType: CommandType.StoredProcedure);

                if (form == null) return NotFound(new { message = "Form not found." });
                if (form.EmployeeManagerId != managerId) return Forbid();
                if ((ExpenseStatus)form.Status != ExpenseStatus.PendingApproval)
                    return BadRequest(new { message = "Only pending forms can be approved." });

                conn.Open();
                using var transaction = conn.BeginTransaction();
                try
                {
                    await conn.ExecuteAsync(
                        "sp_ApproveForm", new { FormId = id }, transaction, commandType: CommandType.StoredProcedure);

                    await conn.ExecuteAsync(
                        "sp_InsertApprovalHistory",
                        new { ExpenseFormId = id, ActionByUserId = managerId, Action = "Approved", Reason = (string?)null, ActionDate = DateTime.Now },
                        transaction,
                        commandType: CommandType.StoredProcedure);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

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

                using var conn = _dapper.CreateConnection();
                var form = await conn.QueryFirstOrDefaultAsync<FormDetailRow>(
                    "sp_GetExpenseFormById", new { FormId = id }, commandType: CommandType.StoredProcedure);

                if (form == null) return NotFound(new { message = "Form not found." });
                if (form.EmployeeManagerId != managerId) return Forbid();
                if ((ExpenseStatus)form.Status != ExpenseStatus.PendingApproval)
                    return BadRequest(new { message = "Only pending forms can have changes requested." });

                conn.Open();
                using var transaction = conn.BeginTransaction();
                try
                {
                    await conn.ExecuteAsync(
                        "sp_RequestChangeForm", new { FormId = id, dto.Reason }, transaction, commandType: CommandType.StoredProcedure);

                    await conn.ExecuteAsync(
                        "sp_InsertApprovalHistory",
                        new { ExpenseFormId = id, ActionByUserId = managerId, Action = "ChangeRequested", dto.Reason, ActionDate = DateTime.Now },
                        transaction,
                        commandType: CommandType.StoredProcedure);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

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
                using var conn = _dapper.CreateConnection();
                using var multi = await conn.QueryMultipleAsync(
                    "sp_GetToBePaid",
                    new { Currency = currency, EmployeeName = employeeName },
                    commandType: CommandType.StoredProcedure);

                var forms = (await multi.ReadAsync<FormRow>()).ToList();
                var items = (await multi.ReadAsync<ItemRow>()).ToList();

                return Ok(ExpenseMapper.ToDtoList(forms, items));
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

                using var conn = _dapper.CreateConnection();
                var form = await conn.QueryFirstOrDefaultAsync<FormDetailRow>(
                    "sp_GetExpenseFormById", new { FormId = id }, commandType: CommandType.StoredProcedure);

                if (form == null) return NotFound(new { message = "Form not found." });
                if ((ExpenseStatus)form.Status != ExpenseStatus.Approved)
                    return BadRequest(new { message = "Only approved forms can be paid." });

                conn.Open();
                using var transaction = conn.BeginTransaction();
                try
                {
                    await conn.ExecuteAsync(
                        "sp_PayForm", new { FormId = id }, transaction, commandType: CommandType.StoredProcedure);

                    await conn.ExecuteAsync(
                        "sp_InsertApprovalHistory",
                        new { ExpenseFormId = id, ActionByUserId = accountantId, Action = "Paid", Reason = (string?)null, ActionDate = DateTime.Now },
                        transaction,
                        commandType: CommandType.StoredProcedure);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                _logger.LogInformation("Expense form {FormId} paid by accountant {AccountantId}", id, accountantId);
                return Ok(new { message = "Expense form marked as paid." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error paying expense form {FormId}", id);
                return StatusCode(500, new { message = "An error occurred while processing payment." });
            }
        }
    }

    public class RejectDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}
