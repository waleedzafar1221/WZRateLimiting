using WZ.RateLimiting.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(); 
builder.Services.AddControllers();
builder.Services.AddWzRateLimiting(options =>
{
   
    options.AddPolicy("bucket-public-api", policy =>
    {
        policy.PerIp().UseTokenBucket().Capacity(2).PerMinute().Refill(1);
    });
    options.AddPolicy("slide-public-api", policy =>
    {
        policy.PerIp().UseSlideWindow().Limit(2).Window(TimeSpan.FromMinutes(1));
    });
    options.AddPolicy("fixed-public-api", policy =>
    {
        policy.PerIp().UseFixedWindow().Limit(2).Window(TimeSpan.FromMinutes(1));
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