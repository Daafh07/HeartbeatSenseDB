using HeartbeatBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest.Exceptions;
using System.Security.Claims;

namespace HeartbeatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly Client _client;
    private readonly ILogger<UsersController> _logger;
    private readonly AuthController _authController;

    public UsersController(Client client, ILogger<UsersController> logger, AuthController authController)
    {
        _client = client;
        _logger = logger;
        _authController = authController;
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.FirstName == null && request.LastName == null && request.Number == null &&
            request.Gender == null && request.DateOfBirth == null && request.Height == null &&
            request.Weight == null && request.BloodType == null)
        {
            return BadRequest(new { message = "No fields to update." });
        }

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

            // Update fields
            if (request.FirstName != null) user.FirstName = request.FirstName;
            if (request.LastName != null) user.LastName = request.LastName;
            if (request.Number != null) user.Number = request.Number;
            if (request.Gender != null) user.Gender = request.Gender;
            if (request.DateOfBirth.HasValue) user.DateOfBirth = request.DateOfBirth.Value;
            if (request.Height.HasValue) user.Height = request.Height;
            if (request.Weight.HasValue) user.Weight = request.Weight;
            if (request.BloodType != null) user.BloodType = request.BloodType;

            // FIX: Use Where clause before Update
            await _client
                .From<User>()
                .Where(u => u.Id == userId)
                .Update(user);

            try
            {
                var payload = await _authController.BuildAuthPayload(user);
                return Ok(payload);
            }
            catch (PostgrestException ex)
            {
                return HandleSupabaseException(ex, "Unable to build user payload.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while building user payload after update.");
                return StatusCode(500, new { message = "Unexpected error while building user payload." });
            }
        }
        catch (PostgrestException ex)
        {
            return HandleSupabaseException(ex, "Unable to update profile.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating profile.");
            return StatusCode(500, new { message = "Unexpected error while updating profile." });
        }
    }

    private IActionResult HandleSupabaseException(PostgrestException exception, string fallbackMessage)
    {
        var statusCode = exception.StatusCode > 0
            ? exception.StatusCode
            : StatusCodes.Status400BadRequest;
        var message = !string.IsNullOrWhiteSpace(exception.Content)
            ? exception.Content
            : fallbackMessage;

        _logger.LogError(exception, "Supabase/PostgREST error: {Message}", message);

        return StatusCode(statusCode, new { message });
    }

}
