using HeartbeatBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using Supabase.Postgrest.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
 
namespace HeartbeatBackend.Controllers;
 
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Client _client;
    private readonly string _jwtSecret;
    private readonly ILogger<AuthController> _logger;
 
    public AuthController(Client client, IConfiguration config, ILogger<AuthController> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger;
        _jwtSecret = config["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set.");
    }
 
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest userInfo)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
 
        try
        {
            var existing = await _client
                .From<User>()
                .Where(u => u.Email == userInfo.Email)
                .Get();
 
            if (existing.Models.Any())
                return BadRequest(new { message = "Email already exists." });
 
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                Email = userInfo.Email,
                Number = userInfo.Number,
                Gender = userInfo.Gender,
                Age = userInfo.Age,
                Password = BCrypt.Net.BCrypt.HashPassword(userInfo.Password),
                CreatedAt = DateTime.UtcNow
            };
 
            await _client.From<User>().Insert(newUser);
 
            return Ok(await BuildAuthPayload(newUser));
        }
        catch (PostgrestException ex)
        {
            return HandleSupabaseException(ex, "Unable to register the user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while registering user {Email}", userInfo.Email);
            return StatusCode(500, new { message = "Unexpected error while registering user." });
        }
    }
 
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginData)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
 
        try
        {
            var response = await _client
                .From<User>()
                .Where(u => u.Email == loginData.Email)
                .Get();
 
            var user = response.Models.FirstOrDefault();
 
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials." });
 
            if (string.IsNullOrWhiteSpace(user.Password) ||
                !BCrypt.Net.BCrypt.Verify(loginData.Password, user.Password))
                return Unauthorized(new { message = "Invalid credentials." });
 
            return Ok(await BuildAuthPayload(user));
        }
        catch (PostgrestException ex)
        {
            return HandleSupabaseException(ex, "Unable to fetch user information.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while logging in user {Email}", loginData.Email);
            return StatusCode(500, new { message = "Unexpected error while logging in." });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> CurrentUser()
    {
        try
        {
            var userIdValue = User.FindFirstValue("id");
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(new { message = "Invalid token." });

            var response = await _client
                .From<User>()
                .Where(u => u.Id == userId)
                .Get();

            var user = response.Models.FirstOrDefault();
            if (user == null)
                return Unauthorized(new { message = "Account no longer exists." });

            return Ok(await BuildAuthPayload(user));
        }
        catch (PostgrestException ex)
        {
            return HandleSupabaseException(ex, "Unable to fetch current user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching current user.");
            return StatusCode(500, new { message = "Unexpected error while fetching current user." });
        }
    }

    private IActionResult HandleSupabaseException(PostgrestException exception, string fallbackMessage)
    {
        var statusCode = exception.StatusCode > 0
            ? exception.StatusCode
            : (int)HttpStatusCode.InternalServerError;
        var message = !string.IsNullOrWhiteSpace(exception.Content)
            ? exception.Content
            : fallbackMessage;

        _logger.LogError(exception, "Supabase/PostgREST error: {Message}", message);

        return StatusCode(statusCode, new { message });
    }

    internal async Task<object> BuildAuthPayload(User user)
    {
        var jwt = GenerateJwt(user);
        var latest = await GetLatestMeasurement(user.Id);

        return new
        {
            token = jwt,
            firstName = user.FirstName,
            lastName = user.LastName,
            email = user.Email,
            number = user.Number,
            gender = user.Gender,
            age = user.Age,
            height = user.Height,
            weight = user.Weight,
            bloodType = user.BloodType,
            latestMeasurement = latest == null
                ? null
                : new
                {
                    value = latest.Value,
                    deviceId = latest.DeviceId,
                    createdAt = latest.CreatedAt
                }
        };
    }

    private string GenerateJwt(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("email", user.Email)
            }),
            Expires = DateTime.UtcNow.AddHours(12),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<Measurement?> GetLatestMeasurement(Guid userId)
    {
        // Fetch devices for user
        var devicesResponse = await _client
            .From<Device>()
            .Where(d => d.UserId == userId)
            .Get();

        var deviceIds = devicesResponse.Models.Select(d => d.Id).ToList();
        if (deviceIds.Count == 0)
            return null;

        Measurement? latest = null;

        foreach (var deviceId in deviceIds)
        {
            var response = await _client
                .From<Measurement>()
                .Where(m => m.DeviceId == deviceId)
                .Order(m => m.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(1)
                .Get();

            var candidate = response.Models.FirstOrDefault();
            if (candidate == null)
                continue;

            if (latest == null || candidate.CreatedAt > latest.CreatedAt)
                latest = candidate;
        }

        return latest;
    }
}
