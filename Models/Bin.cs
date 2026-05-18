using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartBin.Models;

[Table("trash_bin_state_latest")]
public class Bin : BaseModel
{
    [PrimaryKey("simulation_id", false)]
    public string SimulationId { get; set; } = "";

    [PrimaryKey("bin_id", false)]
    public string BinId { get; set; } = "";

    [Column("bin_name")]
    public string binName { get; set; } = "";

    [Column("x")]
    public double X { get; set; }

    [Column("y")]
    public double Y { get; set; }

    [Column("z")]
    public double Z { get; set; }

    [Column("is_full")]
    public bool IsFull { get; set; }

    [Column("fill_level")]
    public int FillLevel { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public int DisplayFillLevel => IsFull ? 90 : FillLevel;

    public string StatusText => (IsFull || FillLevel >= 90) ? "즉시 수거 필요"
        : FillLevel >= 50 ? "수거 필요"
        : "비어있음";

    public string StatusClass => (IsFull || FillLevel >= 90) ? "danger"
        : FillLevel >= 50 ? "warn"
        : "ok";
}
