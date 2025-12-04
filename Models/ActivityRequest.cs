using System.ComponentModel.DataAnnotations;

namespace HeartbeatBackend.Models;

public class ActivityRequest
{
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;
}
