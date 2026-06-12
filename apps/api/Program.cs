using Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        if (context.JsonTypeInfo.Type == typeof(Api.Models.Lodge))
        {
            schema.Required = new HashSet<string> { "id", "name", "createdAt" };
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<AppDbContext>(
    opt => opt.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"))
    .UseSnakeCaseNamingConvention());

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
