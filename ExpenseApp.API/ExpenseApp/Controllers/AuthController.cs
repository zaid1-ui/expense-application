using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using ExpenseApp.Data.Rows;
using ExpenseApp.DTOs;
using ExpenseApp.Enums;
using ExpenseApp.Models;
using ExpenseApp.Services;

namespace ExpenseApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DapperContext _dapper;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(DapperContext dapper, TokenService tokenService, ILogger<AuthController> logger)
        {
            _dapper = dapper;
            _tokenService = tokenService;
            _logger = logger;
        }

        // Signup is Employee-only: Manager/Accountant/Admin accounts are provisioned
        // by seed data, not self-service, so nobody can register their way into a
        // privileged role.
        [HttpGet("managers")]
        public async Task<ActionResult> GetManagers()
        {
            using var conn = _dapper.CreateConnection();
            var managers = await conn.QueryAsync<ManagerRow>(
                "sp_GetManagers", commandType: CommandType.StoredProcedure);

            return Ok(managers);
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.FullName))
                {
                    return BadRequest(new { message = "Username, password, and full name are required." });
                }

                var username = request.Username.Trim();
                if (!Regex.IsMatch(username, "^[a-zA-Z0-9_]{3,20}$"))
                {
                    return BadRequest(new
                    {
                        message = "Username must be 3-20 characters and contain only letters, numbers, and underscores."
                    });
                }

                if (request.FullName.Trim().Length < 2)
                    return BadRequest(new { message = "Please enter your full name." });

                if (request.Password.Length < 6)
                    return BadRequest(new { message = "Password must be at least 6 characters." });

                using var conn = _dapper.CreateConnection();

                var usernameTaken = await conn.ExecuteScalarAsync<bool>(
                    "sp_UsernameExists",
                    new { Username = username },
                    commandType: CommandType.StoredProcedure);
                if (usernameTaken)
                    return BadRequest(new { message = "That username is already taken." });

                var manager = await conn.QueryFirstOrDefaultAsync<ManagerRow>(
                    "sp_GetManagerById",
                    new { ManagerId = request.ManagerId },
                    commandType: CommandType.StoredProcedure);
                if (manager == null)
                    return BadRequest(new { message = "Please select a valid manager." });

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                var newUserId = await conn.ExecuteScalarAsync<int>(
                    "sp_InsertUser",
                    new
                    {
                        Username = username,
                        PasswordHash = passwordHash,
                        FullName = request.FullName.Trim(),
                        Role = (int)UserRole.Employee,
                        ManagerId = manager.Id
                    },
                    commandType: CommandType.StoredProcedure);

                _logger.LogInformation("New employee account registered: {Username} (Id {UserId})", username, newUserId);
                return Ok(new { message = "Account created successfully. You can now log in." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {Username}", request.Username);
                return StatusCode(500, new { message = "An error occurred while creating the account." });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
        {
            try
            {
                using var conn = _dapper.CreateConnection();
                var userRow = await conn.QueryFirstOrDefaultAsync<UserRow>(
                    "sp_GetUserByUsername",
                    new { request.Username },
                    commandType: CommandType.StoredProcedure);

                if (userRow == null || !BCrypt.Net.BCrypt.Verify(request.Password, userRow.PasswordHash))
                {
                    _logger.LogWarning("Failed login attempt for username {Username}", request.Username);
                    return Unauthorized(new { message = "Invalid username or password" });
                }

                var user = new User
                {
                    Id = userRow.Id,
                    Username = userRow.Username,
                    FullName = userRow.FullName,
                    Role = (UserRole)userRow.Role
                };

                var token = _tokenService.GenerateToken(user);

                _logger.LogInformation("User {Username} ({Role}) logged in", user.Username, user.Role);
                return Ok(new LoginResponseDto
                {
                    Token = token,
                    Username = user.Username,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    UserId = user.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username {Username}", request.Username);
                return StatusCode(500, new { message = "An error occurred while logging in." });
            }
        }
    }
}
