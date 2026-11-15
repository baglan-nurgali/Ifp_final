using IoT.Web.Components;
using IoT.Application;
using IoT.Infrastructure;
using IoT.Web.Hubs;
using IoT.Web.State;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISensorSimulator, SensorSimulator>();
builder.Services.AddSingleton<ITelemetryRepository, TelemetryRepository>();
builder.Services.AddSingleton<IAlertEvaluator, AlertEvaluator>();
builder.Services.AddSingleton<TelemetryPipeline>();
builder.Services.AddSingleton<DashboardStateContainer>();
builder.Services.AddSingleton<SensorSimulationWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SensorSimulationWorker>());
builder.Services.AddHostedService<TelemetryBroadcaster>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.MapHub<TelemetryHub>("/hubs/telemetry");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
