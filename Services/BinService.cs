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


}
