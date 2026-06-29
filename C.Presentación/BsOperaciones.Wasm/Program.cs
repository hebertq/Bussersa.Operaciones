using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BsOperaciones;
using MudBlazor.Services;
using BsOperaciones.Services;
using BsOperaciones.Application.Extensions;
using Modelo.Interfaces;
using Modelo.ClasesGenericas;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Threading.Tasks;
using System;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Inject default HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add MudBlazor services
builder.Services.AddMudServices();

// Register FakeSnippetService to avoid local file dependency in browser environment
builder.Services.AddScoped<ICodeSnippetService, FakeSnippetService>();

// Register Application services and Auth policies
builder.Services.AddApplicationServices();
builder.Services.AddAuthorizationPolicy();

// Register UserInfo
builder.Services.AddSingleton<IUserInfo, UserInfo>();

// Set default culture for Nicaragua (using '.' as decimal separator)
var culture = new System.Globalization.CultureInfo("es-NI");
culture.NumberFormat.NumberDecimalSeparator = ".";
culture.NumberFormat.NumberGroupSeparator = ",";
culture.NumberFormat.CurrencyDecimalSeparator = ".";
culture.NumberFormat.CurrencyGroupSeparator = ",";
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

await builder.Build().RunAsync();
