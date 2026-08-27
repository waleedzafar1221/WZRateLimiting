using WZ.RateLimiting.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(); 
builder.Services.AddWzRateLimiting(options =>
{
    options.AddPolicy("public-api", policy =>
    {
        policy.PerIp().Limit(5).PerMinute();
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI(); 
}
app.UseRouting();
app.UseWzRateLimiting();

app.MapGet("/api/products", () => Results.Ok(new[] { "widget", "gadget" }))
    .RequireWzRateLimiting("public-api");

app.Run();