using Microsoft.AspNetCore.StaticFiles;
using SmartBin.Components;
using SmartBin.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var supabaseUrl = builder.Configuration["Supabase:Url"]!;
var supabaseKey = builder.Configuration["Supabase:AnonKey"]!;
var supabase = new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
{
    AutoRefreshToken = false,
    AutoConnectRealtime = false
});
await supabase.InitializeAsync();
builder.Services.AddSingleton(supabase);
builder.Services.AddScoped<BinService>();
builder.Services.AddSingleton<TruckService>();
builder.Services.AddSingleton<AlertService>();
builder.Services.AddHostedService<AlertBackgroundService>();

var app = builder.Build();

// Purge rows left behind by previous simulation runs so the tables don't grow
// unbounded. Each run uses a fresh simulation_id; only the most recent is kept.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var binService = scope.ServiceProvider.GetRequiredService<BinService>();
        var truckService = scope.ServiceProvider.GetRequiredService<TruckService>();
        var binRows = await binService.PurgeOldSimulationsAsync();
        var truckRows = await truckService.PurgeOldSimulationsAsync();
        app.Logger.LogInformation(
            "옛 시뮬레이션 정리 완료: 쓰레기통 {BinRows}행, 수거차 {TruckRows}행 삭제",
            binRows, truckRows);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "옛 시뮬레이션 정리 실패 (앱은 계속 시작됨)");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}


var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".gz"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = ctx =>
    {
        var fileName = ctx.File.Name;
        if (fileName.EndsWith(".gz"))
        {
            ctx.Context.Response.Headers.Append("Content-Encoding", "gzip");
            if (fileName.EndsWith(".wasm.gz"))
                ctx.Context.Response.ContentType = "application/wasm";
            else if (fileName.EndsWith(".js.gz"))
                ctx.Context.Response.ContentType = "application/javascript";
            else
                ctx.Context.Response.ContentType = "application/octet-stream";
        }
    }
});

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
