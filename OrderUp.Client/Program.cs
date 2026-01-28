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
builder.Services.AddScoped<CartState>();

await builder.Build().RunAsync();
