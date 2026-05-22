using SmartBin.Models;

namespace SmartBin.Services;

public class BinService
{
    private readonly Supabase.Client _supabase;

    public BinService(Supabase.Client supabase) => _supabase = supabase;

    public async Task<List<Bin>> GetAllAsync()
    {
        // Step 1: find the latest simulation_id without fetching all rows.
        var latestRow = await _supabase.From<Bin>()
            .Order("updated_at", Postgrest.Constants.Ordering.Descending)
            .Limit(1)
            .Get();
        var latestSimId = latestRow.Models.FirstOrDefault()?.SimulationId;
        if (latestSimId is null) return new();

        // Step 2: fetch only rows for that simulation (still may be many; use high limit).
        var response = await _supabase.From<Bin>()
            .Filter("simulation_id", Postgrest.Constants.Operator.Equals, latestSimId)
            .Order("updated_at", Postgrest.Constants.Ordering.Descending)
            .Limit(10000)
            .Get();

        return response.Models
            .GroupBy(b => b.BinId)
            .Select(g => g.First())
            .ToList();
    }

    public async Task<(int Total, int FullCount, int AvgFillLevel)> GetStatsAsync()
    {
        var bins = await GetAllAsync();
        var avg = bins.Count > 0 ? (int)Math.Round(bins.Average(b => b.FillLevel)) : 0;
        return (bins.Count, bins.Count(b => b.IsFull), avg);
    }

    public async Task<Bin?> GetByIdAsync(string binId)
    {
        var response = await _supabase.From<Bin>()
            .Filter("bin_id", Postgrest.Constants.Operator.Equals, binId)
            .Get();
        return response.Models
            .OrderByDescending(b => b.UpdatedAt)
            .FirstOrDefault();
    }

    // Deletes rows belonging to every simulation except the most recent one.
    // Returns the number of rows removed.
    public async Task<int> PurgeOldSimulationsAsync()
    {
        var all = (await _supabase.From<Bin>().Get()).Models;
        var latestSimId = LatestSimulationId(all);
        if (latestSimId is null) return 0;

        var staleCount = all.Count(b => b.SimulationId != latestSimId);
        if (staleCount == 0) return 0;

        await _supabase.From<Bin>()
            .Filter("simulation_id", Postgrest.Constants.Operator.NotEqual, latestSimId)
            .Delete();
        return staleCount;
    }

    private static string? LatestSimulationId(IEnumerable<Bin> bins) =>
        bins.OrderByDescending(b => b.UpdatedAt).FirstOrDefault()?.SimulationId;
}
