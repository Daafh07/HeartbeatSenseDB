using Microsoft.AspNetCore.Mvc;
using Supabase;
using HeartbeatBackend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
 
namespace HeartbeatBackend.Controllers;
 
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Client _client;
    private readonly string _jwtSecret;
 
    public AuthController(IConfiguration config)
    {
        DotNetEnv.Env.Load();
 
        var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
        var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "";
 
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("SUPABASE_URL or SUPABASE_KEY is not set.");
 
        if (string.IsNullOrWhiteSpace(_jwtSecret))
            throw new InvalidOperationException("JWT_SECRET is not set.");
 
        _client = new Client(url, key);
    }
 
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest userInfo)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
 
        // Check if user exists
        var existing = await _client
            .From<User>()
            .Where(u => u.Email == userInfo.Email)
            .Get();
 
        if (existing.Models.Any())
            return BadRequest(new { message = "Email already exists." });
 
        // Create new user with hashed password
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
 
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginData)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
 
        var response = await _client
            .From<User>()
            .Where(u => u.Email == loginData.Email)
            .Get();
 
        var user = response.Models.FirstOrDefault();
 
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials." });
 
        if (!BCrypt.Net.BCrypt.Verify(loginData.Password, user.Password))
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
}