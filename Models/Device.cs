using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace HeartbeatBackend.Models;

[Table("devices")]
public class Device : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = default!;

    [Column("user_id")]
    public Guid UserId { get; set; }
}
