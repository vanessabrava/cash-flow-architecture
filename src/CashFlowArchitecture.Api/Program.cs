var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "cash-flow-api",
    checkedAt = DateTimeOffset.UtcNow
}));

app.Run();
