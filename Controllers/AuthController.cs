using HeartbeatBackend.Models;
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
                Password = BCrypt.Net.BCrypt.HashPassword(userInfo.Password),
                CreatedAt = DateTime.UtcNow
            };
 
            await _client.From<User>().Insert(newUser);
 
            return Ok(new { message = "User created successfully." });
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
            var jwt = tokenHandler.WriteToken(token);
 
            return Ok(new { token = jwt });
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
}
