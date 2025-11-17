using Microsoft.AspNetCore.Mvc;
using Supabase;
using HeartbeatBackend.Models;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

        _client = new Client(url, key);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User userInfo)
    {
        // Check if user exists
        var existing = await _client.From<User>()
            .Where(u => u.Email == userInfo.Email)
            .Get();

        if (existing.Models.Any())
            return BadRequest("Email already exists.");

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

        return Ok("User created successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] User loginData)
    {
        var response = await _client.From<User>()
            .Where(u => u.Email == loginData.Email)
            .Get();

        var user = response.Models.FirstOrDefault();

        if (user == null)
            return Unauthorized("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(loginData.Password, user.Password))
            return Unauthorized("Invalid credentials.");

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