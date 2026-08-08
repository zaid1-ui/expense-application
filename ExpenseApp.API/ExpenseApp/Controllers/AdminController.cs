using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpenseApp.Data.Rows;
using ExpenseApp.Enums;
using ExpenseApp.Services;

namespace ExpenseApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly DapperContext _dapper;
        private readonly ILogger<AdminController> _logger;

        public AdminController(DapperContext dapper, ILogger<AdminController> logger)
        {
            _dapper = dapper;
            _logger = logger;
        }

        [HttpGet("transactions")]
        public async Task<ActionResult> GetAllTransactions(
            [FromQuery] string? status, [FromQuery] string? employeeName)
        {
            try
            {
                int? statusInt = Enum.TryParse<ExpenseStatus>(status, out var parsedStatus) ? (int)parsedStatus : null;

                using var conn = _dapper.CreateConnection();
                using var multi = await conn.QueryMultipleAsync(
                    "sp_GetAllTransactions",
                    new { Status = statusInt, EmployeeName = employeeName },
                    commandType: CommandType.StoredProcedure);

                var forms = (await multi.ReadAsync<FormRow>()).ToList();
                var items = (await multi.ReadAsync<ItemRow>()).ToList();

                return Ok(ExpenseMapper.ToDtoList(forms, items));
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
                using var conn = _dapper.CreateConnection();
                var history = await conn.QueryAsync<HistoryRow>(
                    "sp_GetApprovalHistory",
                    new { Action = action, EmployeeName = employeeName },
                    commandType: CommandType.StoredProcedure);

                var result = history.Select(h => new
                {
                    h.Id,
                    ExpenseFormId = h.ExpenseFormId,
                    ActionBy = h.ActionBy,
                    ActionByRole = ((UserRole)h.ActionByRole).ToString(),
                    h.Action,
                    h.Reason,
                    h.ActionDate
                });

                return Ok(result);
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
                using var conn = _dapper.CreateConnection();
                var rows = await conn.QueryAsync<StatusReportRow>(
                    "sp_ReportByStatus", commandType: CommandType.StoredProcedure);

                var report = rows.Select(r => new
                {
                    Status = ((ExpenseStatus)r.Status).ToString(),
                    r.FormCount,
                    r.TotalAmount
                });

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
                using var conn = _dapper.CreateConnection();
                var report = await conn.QueryAsync<EmployeeReportRow>(
                    "sp_ReportByEmployee", commandType: CommandType.StoredProcedure);

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
                using var conn = _dapper.CreateConnection();
                var report = await conn.QueryAsync<CategoryReportRow>(
                    "sp_ReportByCategory", commandType: CommandType.StoredProcedure);

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
                using var conn = _dapper.CreateConnection();
                var report = await conn.QueryAsync<MonthlyReportRow>(
                    "sp_ReportByMonthly", commandType: CommandType.StoredProcedure);

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
