using System.Net.Http.Headers;

// Keep this in step with SiloCount in the AppHost.
const int SiloCount = 3;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Under Aspire the silos are injected for service discovery as "silo1".."silo3".
// Started by hand, they are simply on localhost:5001..5003.
var underAspire = builder.Configuration.GetSection("services:silo1").Exists();

if (string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_URLS"]))
{
    builder.WebHost.UseUrls("http://localhost:5080");
}

for (var instance = 1; instance <= SiloCount; instance++)
{
    var port = 5000 + instance;
    var name = $"silo{instance}";

    builder.Services.AddHttpClient(name, client =>
        client.BaseAddress = new Uri(underAspire ? $"http://{name}" : $"http://localhost:{port}"));
}

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseDefaultFiles();
app.UseStaticFiles();

// One pass-through proxy. The browser picks which silo to call with "?silo=N",
// which is what makes the demo click: you can call silo 3 and still get an answer
// from a parcel that lives on silo 1, without knowing or caring where it is.
//
// Going through the server also avoids CORS and keeps the silo's HTTP surface
// exactly as it is. This app is deliberately NOT an Orleans client.
app.Map("/api/{**path}", async (HttpContext context, IHttpClientFactory factory, string path) =>
{
    if (!int.TryParse(context.Request.Query["silo"], out var instance) || instance < 1 || instance > SiloCount)
    {
        instance = 1;
    }

    var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), $"/{path}");

    if (context.Request.ContentLength > 0)
    {
        request.Content = new StreamContent(context.Request.Body);
        request.Content.Headers.ContentType =
            MediaTypeHeaderValue.Parse(context.Request.ContentType ?? "application/json");
    }

    using var client = factory.CreateClient($"silo{instance}");

    try
    {
        using var response = await client.SendAsync(request, context.RequestAborted);
        var body = await response.Content.ReadAsStringAsync(context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(string.IsNullOrWhiteSpace(body) ? "{}" : body, context.RequestAborted);
    }
    catch (Exception exception)
    {
        // Stopping a silo from the Aspire dashboard is part of the demo, so a
        // silo that is not there must not take the control panel down with it.
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            new { error = $"silo-{instance} did not answer", detail = exception.Message },
            context.RequestAborted);
    }
});

app.Run();
