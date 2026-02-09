using Blazored.Toast;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OrderUp.Client;
using OrderUp.Client.Services;
using OrderUp.Client.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<MenuApi>();
builder.Services.AddScoped<OrdersApi>();
builder.Services.AddScoped<AdminApi>();
builder.Services.AddScoped<AuthApi>();
builder.Services.AddScoped<PaymentsApi>();
builder.Services.AddScoped<PendingOrderService>();
builder.Services.AddScoped<RecentOrdersService>();
builder.Services.AddScoped<AppSettingsService>();
builder.Services.AddBlazoredToast();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<CartState>();

var host = builder.Build();

// Initialize app settings before running
var appSettings = host.Services.GetRequiredService<AppSettingsService>();
await appSettings.InitializeAsync();

await host.RunAsync();
