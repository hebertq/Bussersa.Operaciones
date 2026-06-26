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

await builder.Build().RunAsync();
