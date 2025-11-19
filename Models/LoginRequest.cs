using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace HeartbeatBackend.Models;

[Table("users")]
public class LoginRequest : BaseModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}