using System.Net.Http.Headers;

// Keep this in step with SiloCount in the AppHost.
const int SiloCount = 3;

var builder = WebApplication.CreateBuilder(args);

var underAspire = builder.Configuration.GetSection("services:silo1").Exists();

if (underAspire)
{
    builder.AddServiceDefaults();
}
else
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

if (underAspire)
{
    app.MapDefaultEndpoints();
}

app.UseDefaultFiles();
app.UseStaticFiles();

// The browser chooses the silo with ?silo=N; the proxy keeps it free of CORS.
app.Map("/api/{**path}", async (HttpContext context, IHttpClientFactory factory, string path) =>
{
    var selection = context.Request.Query["silo"];
    var instance = 1;
    if (selection.Count > 0 &&
        (!int.TryParse(selection, out instance) || instance < 1 || instance > SiloCount))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = "Choose a silo between 1 and 3." },
            context.RequestAborted);
        return;
    }

    var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), $"/{path}");

    if (context.Request.ContentLength > 0)
    {
        request.Content = new StreamContent(context.Request.Body);
        request.Content.Headers.ContentType =
            MediaTypeHeaderValue.Parse(context.Request.ContentType ?? "application/json");
    }

    using var client = factory.CreateClient($"silo{instance}");
    using var response = await client.SendAsync(request, context.RequestAborted);
    var body = await response.Content.ReadAsStringAsync(context.RequestAborted);

    context.Response.StatusCode = (int)response.StatusCode;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(string.IsNullOrWhiteSpace(body) ? "{}" : body,
        context.RequestAborted);
});

app.Run();
