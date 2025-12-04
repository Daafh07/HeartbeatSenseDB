using HeartbeatBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest.Exceptions;
using System.Net;
using System.Security.Claims;

namespace HeartbeatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeasurementsController : ControllerBase
{
    private readonly Client _client;
    private readonly ILogger<MeasurementsController> _logger;

    public MeasurementsController(Client client, ILogger<MeasurementsController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [Authorize]
    [HttpGet("latest")]
    public async Task<IActionResult> Latest([FromQuery] int limit = 100, [FromQuery] DateTimeOffset? since = null)
    {
        var cappedLimit = Math.Clamp(limit, 1, 500);

        try
        {
            var userIdValue = User.FindFirstValue("id");
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(new { message = "Invalid token." });

            var devicesResponse = await _client
                .From<Device>()
                .Where(d => d.UserId == userId)
                .Get();

            var deviceIds = devicesResponse.Models.Select(d => d.Id).ToList();
            if (deviceIds.Count == 0)
                return Ok(new { items = Array.Empty<object>() });

            var allMeasurements = new List<Measurement>();

            var sinceUtc = since?.UtcDateTime;

            foreach (var deviceId in deviceIds)
            {
                var query = _client
                    .From<Measurement>()
                    .Where(m => m.DeviceId == deviceId);

                if (sinceUtc.HasValue)
                {
                    query = query.Filter(m => m.CreatedAt, Supabase.Postgrest.Constants.Operator.GreaterThan, sinceUtc.Value);
                }

                var response = await query
                    .Order(m => m.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(cappedLimit)
                    .Get();

                allMeasurements.AddRange(response.Models);
            }

            var items = allMeasurements
                .OrderByDescending(m => m.CreatedAt)
                .Take(cappedLimit)
                .Select(m => new
                {
                    id = m.Id,
                    value = m.Value,
                    deviceId = m.DeviceId,
                    createdAt = m.CreatedAt,
                    activityId = m.ActivityId
                })
                .ToList();

            return Ok(new { items });
        }
        catch (PostgrestException ex)
        {
            return HandleSupabaseException(ex, "Unable to fetch measurements.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching measurements.");
            return StatusCode(500, new { message = "Unexpected error while fetching measurements." });
        }
    }

    [Authorize]
    [HttpPut("{id:guid}/activity")]
    public async Task<IActionResult> AttachActivity(Guid id, [FromBody] long activityId)
    {
        try
        {
            var userIdValue = User.FindFirstValue("id");
            if (!Guid.TryParse(userIdValue, out var userId))
                return Unauthorized(new { message = "Invalid token." });

            var devicesResponse = await _client
                .From<Device>()
                .Where(d => d.UserId == userId)
                .Get();

            var deviceIds = devicesResponse.Models.Select(d => d.Id).ToHashSet();
            if (deviceIds.Count == 0)
                return NotFound(new { message = "No devices for this user." });

            var measurementResponse = await _client
                .From<Measurement>()
                .Where(m => m.Id == id)
                .Get();

            var measurement = measurementResponse.Models.FirstOrDefault();
            if (measurement == null || measurement.DeviceId == null || !deviceIds.Contains(measurement.DeviceId))
                return NotFound(new { message = "Measurement not found for this user." });

            var activityResponse = await _client
                .From<Activity>()
                .Where(a => a.Id == activityId)
                .Where(a => a.UserId == userId)
                .Get();

            var activity = activityResponse.Models.FirstOrDefault();
            if (activity == null)
                return NotFound(new { message = "Activity not found for this user." });

            measurement.ActivityId = activityId;

            await _client
                .From<Measurement>()
                .Where(m => m.Id == measurement.Id)
                .Update(measurement);

            return Ok(new
            {
                id = measurement.Id,
                value = measurement.Value,
                deviceId = measurement.DeviceId,
                createdAt = measurement.CreatedAt,
                activityId = measurement.ActivityId
            });
        }
        catch (PostgrestException ex)
        {
            return HandleSupabaseException(ex, "Unable to attach activity to measurement.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while attaching activity to measurement {Id}", id);
            return StatusCode(500, new { message = "Unexpected error while attaching activity to measurement." });
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
