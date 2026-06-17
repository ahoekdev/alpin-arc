using Api.Data;
using Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var seedDemoData = builder.Environment.IsDevelopment();

builder.Services.AddControllers();
builder.Services.AddScoped<ILodgeService, LodgeService>();
builder.Services.AddScoped<IStageService, StageService>();
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(Api.Dtos.LodgeResponseDto))
        {
            schema.Required = new HashSet<string> { "id", "name", "createdAt" };
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<AppDbContext>(
    opt => opt.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"))
    .UseSnakeCaseNamingConvention()
    .UseSeeding((context, created) =>
    {
        if (seedDemoData)
        {
            DemoDataSeeder.Seed(context, created);
        }
    })
    .UseAsyncSeeding(async (context, created, cancellationToken) =>
    {
        if (seedDemoData)
        {
            await DemoDataSeeder.SeedAsync(context, created, cancellationToken);
        }
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });

}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
