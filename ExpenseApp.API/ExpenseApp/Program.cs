using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ExpenseApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// CORS - allow Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register DbContext with SQL Server
builder.Services.AddDbContext<ExpenseAppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication setup
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<ExpenseApp.Services.TokenService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExpenseAppDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        var manager1 = new ExpenseApp.Models.User
        {
            Username = "manager1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
            FullName = "Ali Manager",
            Role = ExpenseApp.Enums.UserRole.Manager
        };
        var manager2 = new ExpenseApp.Models.User
        {
            Username = "manager2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
            FullName = "Sara Manager",
            Role = ExpenseApp.Enums.UserRole.Manager
        };

        db.Users.AddRange(manager1, manager2);
        db.SaveChanges();

        var employee1 = new ExpenseApp.Models.User
        {
            Username = "employee1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
            FullName = "Zaid Employee",
            Role = ExpenseApp.Enums.UserRole.Employee,
            ManagerId = manager1.Id
        };
        var employee2 = new ExpenseApp.Models.User
        {
            Username = "employee2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
            FullName = "Bilal Employee",
            Role = ExpenseApp.Enums.UserRole.Employee,
            ManagerId = manager2.Id
        };

        var accountant = new ExpenseApp.Models.User
        {
            Username = "accountant1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Accountant@123"),
            FullName = "Hina Accountant",
            Role = ExpenseApp.Enums.UserRole.Accountant
        };

        var admin = new ExpenseApp.Models.User
        {
            Username = "admin1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FullName = "Admin User",
            Role = ExpenseApp.Enums.UserRole.Admin
        };

        db.Users.AddRange(employee1, employee2, accountant, admin);
        db.SaveChanges();
    }
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}