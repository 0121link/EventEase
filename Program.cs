using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Caching.Memory;
using EventEase;
using EventEase.Services;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add services with performance optimizations
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    DefaultRequestHeaders = { { "Cache-Control", "no-cache" } }
});

// Add Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add application services
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<TestEventService>(sp => 
    new TestEventService(
        sp.GetRequiredService<ILocalStorageService>(),
        TestEventService.TestScenario.ValidData
    ));
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<AttendanceService>();

// Add error handling
builder.Services.AddScoped<ErrorBoundary>();

// Configure performance options
builder.Services.AddMemoryCache();

await builder.Build().RunAsync();
