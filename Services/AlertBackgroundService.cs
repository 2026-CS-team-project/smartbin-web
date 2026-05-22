namespace SmartBin.Services;

public class AlertBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AlertService _alertService;

    public AlertBackgroundService(IServiceScopeFactory scopeFactory, AlertService alertService)
    {
        _scopeFactory = scopeFactory;
        _alertService = alertService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var binService = scope.ServiceProvider.GetRequiredService<BinService>();
                var bins = await binService.GetAllAsync();
                _alertService.ProcessBins(bins);
            }
            catch { /* 예외가 백그라운드 서비스를 중단시키지 않도록 */ }
        }
    }
}
