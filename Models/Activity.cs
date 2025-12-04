using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Supabase.Postgrest.Models;

namespace HeartbeatBackend.Models;

[Table("activities")]
public class Activity : BaseModel
{
[PrimaryKey("id", false)]
[Column("id", NullValueHandling.Ignore, true, true)]
public long? Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

[Column("created_at", NullValueHandling.Ignore, true, true)]
public DateTime CreatedAt { get; set; }
}
