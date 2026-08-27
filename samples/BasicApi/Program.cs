using WZ.RateLimiting.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(); 
builder.Services.AddControllers();
builder.Services.AddWzRateLimiting(options =>
{
    options.AddPolicy("login", policy =>
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
app.MapControllers();

app.Run();