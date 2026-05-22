using System.Text.RegularExpressions;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SmartBin.Models;

[Table("trash_truck_state_latest")]
public class Truck : BaseModel
{
    [PrimaryKey("simulation_id", false)]
    public string SimulationId { get; set; } = "";

    [PrimaryKey("truck_id", false)]
    public string TruckId { get; set; } = "";

    [Column("x")]
    public double X { get; set; }

    [Column("y")]
    public double Y { get; set; }

    [Column("z")]
    public double Z { get; set; }

    [Column("collected_count")]
    public int CollectedCount { get; set; }

    [Column("max_load")]
    public int MaxLoad { get; set; }

    [Column("status")]
    public string Status { get; set; } = "";

    [Column("destination")]
    public string Destination { get; set; } = "";

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public string DisplayName
    {
        get
        {
            var match = Regex.Match(TruckId, @"\d+$");
            return match.Success ? $"트럭 {match.Value}" : TruckId;
        }
    }

    // 0-based Unity index (sorted alphabetically): TruckId 끝 숫자 - 1
    public int TruckIndex
    {
        get
        {
            var match = Regex.Match(TruckId, @"\d+$");
            return match.Success ? int.Parse(match.Value) - 1 : 0;
        }
    }

    public int LoadPercent => MaxLoad > 0 ? CollectedCount * 100 / MaxLoad : 0;

    public string StatusText => Status switch
    {
        "idle" => "대기",
        "collecting" => "수거중",
        "returning" => "복귀중",
        _ => Status
    };

    public string StatusClass => Status switch
    {
        "idle" => "off",
        "collecting" => "ok",
        "returning" => "warn",
        _ => "off"
    };
}
