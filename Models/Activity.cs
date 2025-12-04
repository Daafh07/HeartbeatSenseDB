using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace HeartbeatBackend.Models;

[Table("activities")]
public class Activity : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public long Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
