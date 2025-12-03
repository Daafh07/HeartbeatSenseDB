using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeartbeatBackend.Models;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [RegularExpression(@"^\d+$", ErrorMessage = "Number must be numeric.")]
    public string? Number { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    public decimal? Height { get; set; }

    public decimal? Weight { get; set; }

    [MaxLength(10)]
    public string? BloodType { get; set; }
}
