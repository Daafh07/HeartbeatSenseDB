using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeartbeatBackend.Models;
 
public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d+$", ErrorMessage = "Number must be numeric.")]
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [Range(1, 120)]
    public int Age { get; set; }
}
