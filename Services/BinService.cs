using SmartBin.Models;

namespace SmartBin.Services;

public class BinService
{
    private readonly Supabase.Client _supabase;

    public BinService(Supabase.Client supabase) => _supabase = supabase;

    public async Task<List<Bin>> GetAllAsync()
    {
        var response = await _supabase.From<Bin>().Get();
        return response.Models
            .GroupBy(b => b.BinId)
            .Select(g => g.OrderByDescending(b => b.UpdatedAt).First())
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
