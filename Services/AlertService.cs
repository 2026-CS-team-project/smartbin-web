using SmartBin.Models;

namespace SmartBin.Services;

public class AlertService
{
    private const int MaxAlerts = 100;

    private readonly List<Alert> _alerts = new();
    private readonly Dictionary<string, int> _prevLevels = new();
    private readonly object _lock = new();
    private int _unreadCount;

    public event Action? OnChanged;

    public IReadOnlyList<Alert> GetAlerts()
    {
        lock (_lock) return _alerts.ToList();
    }

    public int UnreadCount
    {
        get { lock (_lock) return _unreadCount; }
    }

    public void ProcessBins(IEnumerable<Bin> bins)
    {
        bool changed = false;

        lock (_lock)
        {
            foreach (var bin in bins)
            {
                int level = bin.FillLevel;

                if (_prevLevels.TryGetValue(bin.BinId, out int prev))
                {
                    // 높은 임계값 우선 체크 (90% → 70% 순)
                    if (prev < 90 && level >= 90)
                    {
                        _alerts.Insert(0, new Alert(bin.BinId, bin.DisplayName, level, 90, DateTime.Now));
                        _unreadCount++;
                        changed = true;
                    }
                    else if (prev < 70 && level >= 70)
                    {
                        _alerts.Insert(0, new Alert(bin.BinId, bin.DisplayName, level, 70, DateTime.Now));
                        _unreadCount++;
                        changed = true;
                    }
                }

                _prevLevels[bin.BinId] = level;
            }

            while (_alerts.Count > MaxAlerts)
                _alerts.RemoveAt(_alerts.Count - 1);
        }

        if (changed)
            OnChanged?.Invoke();
    }

    public void MarkAllRead()
    {
        lock (_lock) _unreadCount = 0;
        OnChanged?.Invoke();
    }
}
