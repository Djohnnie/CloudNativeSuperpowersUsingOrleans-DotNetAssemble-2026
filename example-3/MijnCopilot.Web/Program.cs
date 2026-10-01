using System.Security.Claims;
using Auth0.AspNetCore.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using MijnCopilot.Application.DependencyInjection;
using MijnCopilot.Application.User.Commands;
using MijnCopilot.Web.Components;
using MijnCopilot.Web.Helpers;
using MudBlazor.Services;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);

var storageConnectionString = builder.Configuration.GetConnectionString("tables")
    ?? builder.Configuration.GetValue<string>("BLOB_CONNECTION_STRING")
    ?? throw new InvalidOperationException(
        "Configure the Azure Storage connection string as ConnectionStrings:tables or BLOB_CONNECTION_STRING.");

builder.UseOrleansClient(clientBuilder =>
{
    clientBuilder
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "mijn-copilot";
            options.ServiceId = "mijn-copilot";
        })
        .UseAzureStorageClustering(options =>
        {
            options.TableServiceClient = new Azure.Data.Tables.TableServiceClient(storageConnectionString);
        });
});

builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration.GetValue<string>("AUTH0_DOMAIN")!;
    options.ClientId = builder.Configuration.GetValue<string>("AUTH0_CLIENTID")!;
    options.ClientSecret = builder.Configuration.GetValue<string>("AUTH0_CLIENTSECRET");
});

builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.Events = new CookieAuthenticationEvents
        {
            OnSignedIn = async context =>
            {
                var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return;

                var name = context.Principal?.FindFirst("nickname")?.Value
                           ?? context.Principal?.Identity?.Name
                           ?? string.Empty;
                var email = context.Principal?.FindFirst("email")?.Value ?? string.Empty;

                var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();
                await mediator.Send(new UserLoginCommand { UserId = userId, Name = name, Email = email });
            }
        };
    });

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddScoped<NavigationHelper>();
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages();

app.Run();