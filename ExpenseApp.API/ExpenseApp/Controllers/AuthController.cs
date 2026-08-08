using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseApp.Data;
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
        private readonly ExpenseAppDbContext _context;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ExpenseAppDbContext context, TokenService tokenService, ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        // Signup is Employee-only: Manager/Accountant/Admin accounts are provisioned
        // by seed data, not self-service, so nobody can register their way into a
        // privileged role.
        [HttpGet("managers")]
        public async Task<ActionResult> GetManagers()
        {
            var managers = await _context.Users
                .Where(u => u.Role == UserRole.Manager)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();

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

                var usernameTaken = await _context.Users.AnyAsync(u => u.Username == username);
                if (usernameTaken)
                    return BadRequest(new { message = "That username is already taken." });

                var manager = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.ManagerId && u.Role == UserRole.Manager);
                if (manager == null)
                    return BadRequest(new { message = "Please select a valid manager." });

                var user = new User
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FullName = request.FullName.Trim(),
                    Role = UserRole.Employee,
                    ManagerId = manager.Id
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New employee account registered: {Username}", user.Username);
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
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Failed login attempt for username {Username}", request.Username);
                    return Unauthorized(new { message = "Invalid username or password" });
                }

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