using SmartBin.Models;

namespace SmartBin.Services;

public class TruckService
{
    private readonly Supabase.Client _supabase;

    public TruckService(Supabase.Client supabase) => _supabase = supabase;

    public async Task<List<Truck>> GetAllAsync()
    {
        var response = await _supabase.From<Truck>().Get();
        return response.Models
            .GroupBy(t => t.TruckId)
            .Select(g => g.OrderByDescending(t => t.UpdatedAt).First())
            .ToList();
    }

    public async Task<int> GetTotalCollectedKgAsync()
    {
        var trucks = await GetAllAsync();
        return trucks.Sum(t => t.CollectedCount) * 50;
    }

    public async Task<Truck?> GetByIdAsync(string truckId)
    {
        var response = await _supabase.From<Truck>()
            .Filter("truck_id", Postgrest.Constants.Operator.Equals, truckId)
            .Get();
        return response.Models
            .OrderByDescending(t => t.UpdatedAt)
            .FirstOrDefault();
    }
}