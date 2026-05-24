using SmartBin.Models;

namespace SmartBin.Services;

public class TruckService
{
    private readonly Supabase.Client _supabase;

    public TruckService(Supabase.Client supabase) => _supabase = supabase;

    public async Task<List<Truck>> GetAllAsync()
    {
        var latestRow = await _supabase.From<Truck>()
            .Order("updated_at", Postgrest.Constants.Ordering.Descending)
            .Limit(1)
            .Get();
        var latestSimId = latestRow.Models.FirstOrDefault()?.SimulationId;
        if (latestSimId is null) return new();

        var response = await _supabase.From<Truck>()
            .Filter("simulation_id", Postgrest.Constants.Operator.Equals, latestSimId)
            .Order("updated_at", Postgrest.Constants.Ordering.Descending)
            .Limit(10000)
            .Get();

        return response.Models
            .GroupBy(t => t.TruckId)
            .Select(g => g.First())
            .ToList();
    }

    // ── Simulation-level accumulated state ──────────────────────────────────
    // All fields reset when simulation_id changes.
    private string? _trackedSimId;
    private int _lastSnapshotKg;
    private int _accumulatedKg;

    // Per-truck: last known CollectedCount and completed trip count.
    private Dictionary<string, int> _truckLastCount = new();
    private Dictionary<string, int> _truckTripCounts = new();

    // Returns how many completed collection trips a truck has made this simulation.
    public int GetTripCount(string truckId) =>
        _truckTripCounts.TryGetValue(truckId, out var c) ? c : 0;

    private void ResetSimState(List<Truck> trucks, string simId)
    {
        _trackedSimId = simId;
        _lastSnapshotKg = trucks.Sum(t => t.CollectedCount) * 50;
        _accumulatedKg = 0;
        _truckLastCount = trucks.ToDictionary(t => t.TruckId, t => t.CollectedCount);
        _truckTripCounts = trucks.ToDictionary(t => t.TruckId, _ => 0);
    }

    private void UpdateTripCounts(List<Truck> trucks)
    {
        foreach (var truck in trucks)
        {
            if (_truckLastCount.TryGetValue(truck.TruckId, out var prev) && truck.CollectedCount < prev)
            {
                // CollectedCount dropped → truck dumped its load → completed trip.
                _truckTripCounts[truck.TruckId] = _truckTripCounts.GetValueOrDefault(truck.TruckId) + 1;
            }
            _truckLastCount[truck.TruckId] = truck.CollectedCount;
        }
    }

    public async Task<int> GetTotalCollectedKgAsync()
    {
        var latestRow = await _supabase.From<Truck>()
            .Order("updated_at", Postgrest.Constants.Ordering.Descending)
            .Limit(1)
            .Get();
        var latestSimId = latestRow.Models.FirstOrDefault()?.SimulationId;
        if (latestSimId is null) return _accumulatedKg;

        var response = await _supabase.From<Truck>()
            .Filter("simulation_id", Postgrest.Constants.Operator.Equals, latestSimId)
            .Order("updated_at", Postgrest.Constants.Ordering.Descending)
            .Limit(10000)
            .Get();

        var trucks = response.Models
            .GroupBy(t => t.TruckId)
            .Select(g => g.First())
            .ToList();

        if (_trackedSimId != latestSimId)
        {
            ResetSimState(trucks, latestSimId);
        }
        else
        {
            UpdateTripCounts(trucks);

            var currentKg = trucks.Sum(t => t.CollectedCount) * 50;
            if (currentKg > _lastSnapshotKg)
            {
                _accumulatedKg += currentKg - _lastSnapshotKg;
                _lastSnapshotKg = currentKg;
            }
            else
            {
                _lastSnapshotKg = currentKg;
            }
        }

        return _accumulatedKg;
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
