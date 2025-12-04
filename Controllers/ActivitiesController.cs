using HeartbeatBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest.Exceptions;
using System.Net;

namespace HeartbeatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly Client _client;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(Client client, ILogger<ActivitiesController> logger)
    {
        _client = client;
        _logger = logger;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var response = await _client
                .From<Activity>()
                .Order(a => a.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            return Ok(response.Models.Select(a => new
            {
                id = a.Id,
                title = a.Title,
                type = a.Type,
                description = a.Description,
                createdAt = a.CreatedAt
            }));
        }
        catch (PostgrestException ex)
        {
            return HandleSupabaseException(ex, "Unable to fetch activities.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while listing activities.");
            return StatusCode(500, new { message = "Unexpected error while listing activities." });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ActivityRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var newActivity = new Activity
            {
                Title = request.Title,
                Type = request.Type,
                Description = request.Description
            };

            var response = await _client.From<Activity>().Insert(newActivity);
            var created = response.Models.First();

            return Ok(new
            {
                id = created.Id,
                title = created.Title,
                type = created.Type,
                description = created.Description,
                createdAt = created.CreatedAt
            });
        }
        catch (PostgrestException ex)
        {
            return HandleSupabaseException(ex, "Unable to create activity.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating activity.");
            return StatusCode(500, new { message = "Unexpected error while creating activity." });
        }
    }

    [Authorize]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] ActivityRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var existing = await _client
                .From<Activity>()
                .Where(a => a.Id == id)
                .Get();

            var activity = existing.Models.FirstOrDefault();
            if (activity == null)
                return NotFound(new { message = "Activity not found." });

            activity.Title = request.Title;
            activity.Type = request.Type;
            activity.Description = request.Description;

            await _client
                .From<Activity>()
                .Where(a => a.Id == id)
                .Update(activity);

            return Ok(new
            {
                id = activity.Id,
                title = activity.Title,
                type = activity.Type,
                description = activity.Description,
                createdAt = activity.CreatedAt
            });
        }
        catch (PostgrestException ex)
        {
            return HandleSupabaseException(ex, "Unable to update activity.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating activity.");
            return StatusCode(500, new { message = "Unexpected error while updating activity." });
        }
    }

    private IActionResult HandleSupabaseException(PostgrestException exception, string fallbackMessage)
    {
        var statusCode = exception.StatusCode > 0
            ? exception.StatusCode
            : (int)HttpStatusCode.BadRequest;
        var message = !string.IsNullOrWhiteSpace(exception.Content)
            ? exception.Content
            : fallbackMessage;

        _logger.LogError(exception, "Supabase/PostgREST error: {Message}", message);

        return StatusCode(statusCode, new { message });
    }
}
