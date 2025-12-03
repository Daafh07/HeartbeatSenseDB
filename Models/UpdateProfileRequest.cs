using System.ComponentModel.DataAnnotations;

namespace HeartbeatBackend.Models;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Range(10000000, long.MaxValue, ErrorMessage = "Number must be numeric.")]
    public long? Number { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [Range(1, 120)]
    public int? Age { get; set; }

    public decimal? Height { get; set; }

    public decimal? Weight { get; set; }

    [MaxLength(10)]
    public string? BloodType { get; set; }
}
