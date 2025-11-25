using Supabase.Postgrest.Models;

using Supabase.Postgrest.Attributes;
 
namespace HeartbeatBackend.Models;
 
[Table("users")]

public class User : BaseModel

{

    [PrimaryKey("id", false)]

    [Column("id")]

    public Guid Id { get; set; }
 
    [Column("first_name")]

    public string FirstName { get; set; } = default!;
 
    [Column("last_name")]

    public string LastName { get; set; } = default!;
 
    [Column("email")]
    public string Email { get; set; } = default!;
 
    [Column("password")]
    public string Password { get; set; } = default!;

    [Column("number")]
    public long Number { get; set; }

    [Column("gender")]
    public string Gender { get; set; } = default!;

    [Column("age")]
    public int Age { get; set; }
 
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

}

 
