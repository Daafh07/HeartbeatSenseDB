using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace HeartbeatBackend.Models;

[Table("users")]
public class User : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("password")]
    public string Password { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}