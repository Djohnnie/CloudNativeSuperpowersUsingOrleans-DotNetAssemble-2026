using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Two ways to run this control panel, matching the silo:
//   * under Aspire, which injects the silo endpoints for service discovery;
//   * standalone, where the silo of instance 1 is expected on http://localhost:5001.
var underAspire = builder.Configuration.GetSection("services:silo").Exists();

if (underAspire)
{
    builder.AddServiceDefaults();
}
else
{
    builder.WebHost.UseUrls("http://localhost:5080");
}

// Service discovery resolves "http://silo" to one of the silo replicas, so every
// request can land on a different silo. Standalone we talk to instance 1 directly.
builder.Services.AddHttpClient("silo", client =>
    client.BaseAddress = new Uri(underAspire ? "http://silo" : "http://localhost:5001"));

var app = builder.Build();

if (underAspire)
{
    app.MapDefaultEndpoints();
}

app.UseDefaultFiles();
app.UseStaticFiles();

// A single pass-through proxy: the browser calls /api/heroes/ironman and this
// forwards it to the silo. Going through the server avoids CORS entirely and keeps
// the silo endpoints exactly as they are.
app.Map("/api/{**path}", async (HttpContext context, IHttpClientFactory factory, string path) =>
{
    var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), $"/{path}");

    if (context.Request.ContentLength > 0)
    {
        request.Content = new StreamContent(context.Request.Body);
        request.Content.Headers.ContentType =
            MediaTypeHeaderValue.Parse(context.Request.ContentType ?? "application/json");
    }

    using var client = factory.CreateClient("silo");
    using var response = await client.SendAsync(request, context.RequestAborted);
    var body = await response.Content.ReadAsStringAsync(context.RequestAborted);

    context.Response.StatusCode = (int)response.StatusCode;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(string.IsNullOrWhiteSpace(body) ? "{}" : body,
        context.RequestAborted);
});

app.Run();
