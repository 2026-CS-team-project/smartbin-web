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
builder.Services.AddScoped<TruckService>();

var app = builder.Build();

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
