using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace HeartbeatBackend.Models;

[Table("measurements")]
public class Measurement : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("value")]
    public string Value { get; set; } = default!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("device_id")]
    public string? DeviceId { get; set; }

    // Optional link to an activity (if the DB has an activity_id column)
    [Column("activity_id")]
    public long? ActivityId { get; set; }
}
