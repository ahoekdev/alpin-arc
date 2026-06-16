# API Contract And Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert the ASP.NET Core API to an explicit DTO-only contract, normalize validation/error responses, and add PostgreSQL-backed integration tests that protect the API behavior.

**Architecture:** Keep the existing MVC controller style. EF Core entities remain persistence models only; controllers accept request DTOs and return response DTOs. Integration tests run the real API through `WebApplicationFactory` with PostgreSQL via Testcontainers so routing, model binding, EF constraints, and Npgsql-specific behavior are covered.

**Tech Stack:** ASP.NET Core MVC on .NET 10, EF Core 10, Npgsql/PostgreSQL, OpenAPI, xUnit, Microsoft.AspNetCore.Mvc.Testing, Testcontainers for PostgreSQL.

---

## Decisions Captured

- Use explicit DTOs everywhere, including lodges.
- Do not accept or return EF Core entities from API endpoints.
- Keep `CreatedAt` server/database-owned. It must not appear in create/update request DTOs.
- Expose API timestamps as `DateTimeOffset`.
- Reject invalid stage requests before persistence:
  - `StartLodgeId <= 0`
  - `EndLodgeId <= 0`
  - `StartLodgeId == EndLodgeId`
  - `DurationMinutes <= 0`
  - `DistanceMeters <= 0`
- Keep the current `Stage` table for MVP.
- Allow reversed stages (`A -> B` and `B -> A`) as distinct rows for now.
- Treat duplicated distance between reversed stages as temporary; defer a future `Route` plus directional variant model until shared route-level fields become important.
- Return `ProblemDetails` / `ValidationProblemDetails`-style responses for validation, domain, and database constraint errors.
- Add integration tests using PostgreSQL via Testcontainers, not EF InMemory or SQLite.

## Current Code Context

- API entry point: `apps/api/Program.cs`
- EF context: `apps/api/Data/ApplicationDbContext.cs`
- Persistence models:
  - `apps/api/Models/Lodge.cs`
  - `apps/api/Models/Stage.cs`
- Existing controllers:
  - `apps/api/Controllers/LodgesController.cs`
  - `apps/api/Controllers/StagesController.cs`
- Existing DTOs:
  - `apps/api/Dtos/LodgeDtos.cs`
  - `apps/api/Dtos/StageDtos.cs`
- Existing migrations:
  - `apps/api/Migrations/20260612053840_InitialCreate.cs`
  - `apps/api/Migrations/20260612192742_StagesCreate.cs`
- No test project currently exists.

## File Structure

- Modify: `apps/api/Program.cs`
  - Remove Lodge entity OpenAPI schema workaround after Lodge DTOs exist.
  - Expose `Program` to integration tests with `public partial class Program`.
- Modify: `apps/api/Dtos/LodgeDtos.cs`
  - Replace the current summary-only DTO file with create/update/response/summary DTOs.
- Modify: `apps/api/Dtos/StageDtos.cs`
  - Convert mutable request class to sealed record request type with init properties.
  - Add lodge ID validation.
  - Change response timestamp to `DateTimeOffset`.
- Modify: `apps/api/Models/Lodge.cs`
  - Change `CreatedAt` to `DateTimeOffset`.
- Modify: `apps/api/Models/Stage.cs`
  - Change `CreatedAt` to `DateTimeOffset`.
- Modify: `apps/api/Data/ApplicationDbContext.cs`
  - Keep existing constraints.
  - Consider adding max-length/index constraints only if required by tests or current schema drift.
- Modify: `apps/api/Controllers/LodgesController.cs`
  - Use Lodge DTOs for all actions.
  - Add `AsNoTracking()` to read queries.
  - Handle referenced-lodge delete conflicts with `409 ProblemDetails`.
- Modify: `apps/api/Controllers/StagesController.cs`
  - Use sealed DTOs.
  - Add `AsNoTracking()` to read projections.
  - Add same-lodge validation.
  - Return `ProblemDetails` for unique/FK constraint failures.
  - Remove unnecessary `EntityState.Modified` on tracked update entity.
- Modify: `apps/api/api.csproj`
  - Remove packages not needed by the API at runtime/build time.
  - Keep provider/design packages needed by EF migrations.
- Create: `apps/api.Tests/api.Tests.csproj`
  - xUnit integration test project.
- Create: `apps/api.Tests/ApiFactory.cs`
  - Test server factory that replaces the API database with a Testcontainers PostgreSQL connection.
- Create: `apps/api.Tests/ApiTestBase.cs`
  - Shared HTTP client, database reset/seed helpers, and JSON helpers.
- Create: `apps/api.Tests/LodgesApiTests.cs`
  - Contract and error behavior tests for lodge endpoints.
- Create: `apps/api.Tests/StagesApiTests.cs`
  - Contract, validation, and database constraint tests for stage endpoints.

---

### Task 1: Make The API Testable From WebApplicationFactory

**Files:**
- Modify: `apps/api/Program.cs`
- Create: `apps/api.Tests/api.Tests.csproj`

- [ ] **Step 1: Add the API test project file**

Create `apps/api.Tests/api.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.9" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.0" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.9.0" />
    <PackageReference Include="xunit.v3" Version="3.2.0" />
    <PackageReference Include="xunit.v3.runner.visualstudio" Version="3.2.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\api\api.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Expose Program for integration tests**

At the end of `apps/api/Program.cs`, after `app.Run();`, add:

```csharp
public partial class Program;
```

- [ ] **Step 3: Build the test project**

Run:

```bash
dotnet build apps/api.Tests/api.Tests.csproj
```

Expected: build succeeds. Package vulnerability warnings may still appear until Task 8.

- [ ] **Step 4: Commit**

```bash
git add apps/api/Program.cs apps/api.Tests/api.Tests.csproj
git commit -m "test: add api integration test project"
```

---

### Task 2: Add PostgreSQL Testcontainers Infrastructure

**Files:**
- Create: `apps/api.Tests/ApiFactory.cs`
- Create: `apps/api.Tests/ApiTestBase.cs`

- [ ] **Step 1: Create the test factory**

Create `apps/api.Tests/ApiFactory.cs`:

```csharp
using Api.Data;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("alpinarc_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString())
                    .UseSnakeCaseNamingConvention());
        });
    }
}
```

- [ ] **Step 2: Create the shared test base**

Create `apps/api.Tests/ApiTestBase.cs`:

```csharp
using System.Net.Http.Json;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public abstract class ApiTestBase : IClassFixture<ApiFactory>, IAsyncLifetime
{
    protected ApiTestBase(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected ApiFactory Factory { get; }

    protected HttpClient Client { get; }

    public async ValueTask InitializeAsync()
    {
        await ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected async Task<(Lodge Start, Lodge End)> SeedTwoLodgesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var start = new Lodge { Name = "Rifugio Auronzo" };
        var end = new Lodge { Name = "Rifugio Lavaredo" };

        db.Lodges.AddRange(start, end);
        await db.SaveChangesAsync();

        return (start, end);
    }

    protected async Task<Stage> SeedStageAsync(long startLodgeId, long endLodgeId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stage = new Stage
        {
            StartLodgeId = startLodgeId,
            EndLodgeId = endLodgeId,
            DurationMinutes = 90,
            DistanceMeters = 4200,
        };

        db.Stages.Add(stage);
        await db.SaveChangesAsync();

        return stage;
    }

    protected static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(value);
        return value;
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Stages.ExecuteDeleteAsync();
        await db.Lodges.ExecuteDeleteAsync();
    }
}
```

- [ ] **Step 3: Build the test project**

Run:

```bash
dotnet build apps/api.Tests/api.Tests.csproj
```

Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add apps/api.Tests/ApiFactory.cs apps/api.Tests/ApiTestBase.cs
git commit -m "test: add postgres api test infrastructure"
```

---

### Task 3: Write Failing Lodge Contract Tests

**Files:**
- Create: `apps/api.Tests/LodgesApiTests.cs`

- [ ] **Step 1: Create failing tests for desired lodge behavior**

Create `apps/api.Tests/LodgesApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Api.Tests;

public sealed class LodgesApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task PostLodge_IgnoresOverpostedServerOwnedFields()
    {
        var response = await Client.PostAsJsonAsync("/api/lodges", new
        {
            id = 999,
            name = "Rifugio Locatelli",
            createdAt = "2000-01-01T00:00:00Z",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.NotEqual(999, root.GetProperty("id").GetInt64());
        Assert.Equal("Rifugio Locatelli", root.GetProperty("name").GetString());
        Assert.True(root.TryGetProperty("createdAt", out var createdAt));
        Assert.NotEqual("2000-01-01T00:00:00Z", createdAt.GetString());
    }

    [Fact]
    public async Task GetLodges_ReturnsResponseDtosOrderedByName()
    {
        await Client.PostAsJsonAsync("/api/lodges", new { name = "Zeta Hut" });
        await Client.PostAsJsonAsync("/api/lodges", new { name = "Alpha Hut" });

        var response = await Client.GetAsync("/api/lodges");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var lodges = document.RootElement;

        Assert.Equal("Alpha Hut", lodges[0].GetProperty("name").GetString());
        Assert.Equal("Zeta Hut", lodges[1].GetProperty("name").GetString());
        Assert.True(lodges[0].TryGetProperty("id", out _));
        Assert.True(lodges[0].TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task PutLodge_DoesNotAllowChangingIdOrCreatedAt()
    {
        var create = await Client.PostAsJsonAsync("/api/lodges", new { name = "Original" });
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync()).RootElement;
        var id = created.GetProperty("id").GetInt64();
        var createdAt = created.GetProperty("createdAt").GetString();

        var update = await Client.PutAsJsonAsync($"/api/lodges/{id}", new
        {
            id = id + 100,
            name = "Updated",
            createdAt = "2000-01-01T00:00:00Z",
        });

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var get = await Client.GetAsync($"/api/lodges/{id}");
        var updated = JsonDocument.Parse(await get.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(id, updated.GetProperty("id").GetInt64());
        Assert.Equal("Updated", updated.GetProperty("name").GetString());
        Assert.Equal(createdAt, updated.GetProperty("createdAt").GetString());
    }

    [Fact]
    public async Task DeleteLodge_WhenReferencedByStage_ReturnsConflictProblemDetails()
    {
        var (start, end) = await SeedTwoLodgesAsync();
        await SeedStageAsync(start.Id, end.Id);

        var response = await Client.DeleteAsync($"/api/lodges/{start.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await ReadJsonAsync<ProblemDetails>(response);
        Assert.Equal(409, problem.Status);
        Assert.Contains("used by stages", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Run lodge tests to verify they fail**

Run:

```bash
dotnet test apps/api.Tests/api.Tests.csproj --filter FullyQualifiedName~LodgesApiTests
```

Expected: at least one test fails because lodges still bind and return `Lodge` entities directly, delete conflict handling is missing, or `CreatedAt` is still overpostable.

- [ ] **Step 3: Commit failing tests only if the team accepts red commits**

Preferred for this repo: do not commit failing tests alone. Continue to Task 4, then commit tests and implementation together.

---

### Task 4: Implement Lodge DTO Contract And Delete Conflict Handling

**Files:**
- Modify: `apps/api/Dtos/LodgeDtos.cs`
- Modify: `apps/api/Controllers/LodgesController.cs`
- Modify: `apps/api/Program.cs`

- [ ] **Step 1: Replace Lodge DTOs**

Replace `apps/api/Dtos/LodgeDtos.cs` with:

```csharp
namespace Api.Dtos;

using System.ComponentModel.DataAnnotations;

/// <summary>Payload for creating a lodge.</summary>
public sealed record CreateLodgeRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }
}

/// <summary>Payload for updating a lodge.</summary>
public sealed record UpdateLodgeRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }
}

/// <summary>Represents a lodge returned by the API.</summary>
public sealed record LodgeResponseDto(
    long Id,
    string Name,
    DateTimeOffset CreatedAt);

/// <summary>Represents a lodge nested inside another API response.</summary>
public sealed record LodgeSummaryDto(long Id, string Name);
```

- [ ] **Step 2: Replace LodgesController**

Replace `apps/api/Controllers/LodgesController.cs` with:

```csharp
using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Controllers
{
    [Route("api/lodges")]
    [ApiController]
    public class LodgesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LodgesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LodgeResponseDto>>> GetLodges()
        {
            return await LodgeResponseQuery()
                .OrderBy(lodge => lodge.Name)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LodgeResponseDto>> GetLodge(long id)
        {
            var lodge = await LodgeResponseQuery()
                .FirstOrDefaultAsync(lodge => lodge.Id == id);

            if (lodge == null)
            {
                return NotFound();
            }

            return lodge;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutLodge(long id, UpdateLodgeRequest request)
        {
            var lodge = await _context.Lodges.FindAsync(id);

            if (lodge == null)
            {
                return NotFound();
            }

            lodge.Name = request.Name;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<LodgeResponseDto>> PostLodge(CreateLodgeRequest request)
        {
            var lodge = new Lodge
            {
                Name = request.Name,
            };

            _context.Lodges.Add(lodge);
            await _context.SaveChangesAsync();

            var response = await LodgeResponseQuery()
                .FirstAsync(savedLodge => savedLodge.Id == lodge.Id);

            return CreatedAtAction(nameof(GetLodge), new { id = lodge.Id }, response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLodge(long id)
        {
            var lodge = await _context.Lodges.FindAsync(id);

            if (lodge == null)
            {
                return NotFound();
            }

            _context.Lodges.Remove(lodge);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Lodge is used by stages.",
                    detail: "Cannot delete a lodge that is used by stages.");
            }

            return NoContent();
        }

        private IQueryable<LodgeResponseDto> LodgeResponseQuery()
        {
            return _context.Lodges
                .AsNoTracking()
                .Select(lodge => new LodgeResponseDto(
                    lodge.Id,
                    lodge.Name,
                    lodge.CreatedAt));
        }

        private static bool IsForeignKeyViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException postgresException
                && postgresException.SqlState == PostgresErrorCodes.ForeignKeyViolation;
        }
    }
}
```

- [ ] **Step 3: Remove the Lodge entity OpenAPI workaround**

In `apps/api/Program.cs`, replace:

```csharp
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
```

with:

```csharp
builder.Services.AddOpenApi();
```

- [ ] **Step 4: Run lodge tests**

Run:

```bash
dotnet test apps/api.Tests/api.Tests.csproj --filter FullyQualifiedName~LodgesApiTests
```

Expected: all `LodgesApiTests` pass.

- [ ] **Step 5: Commit**

```bash
git add apps/api/Dtos/LodgeDtos.cs apps/api/Controllers/LodgesController.cs apps/api/Program.cs apps/api.Tests/LodgesApiTests.cs
git commit -m "feat: use lodge dtos and handle delete conflicts"
```

---

### Task 5: Write Failing Stage Contract And Validation Tests

**Files:**
- Create: `apps/api.Tests/StagesApiTests.cs`

- [ ] **Step 1: Create failing tests for desired stage behavior**

Create `apps/api.Tests/StagesApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Api.Tests;

public sealed class StagesApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task PostStage_WithSameStartAndEndLodge_ReturnsValidationProblem()
    {
        var (start, _) = await SeedTwoLodgesAsync();

        var response = await Client.PostAsJsonAsync("/api/stages", new
        {
            startLodgeId = start.Id,
            endLodgeId = start.Id,
            durationMinutes = 60,
            distanceMeters = 3000,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadJsonAsync<ValidationProblemDetails>(response);
        Assert.Contains(nameof(problem.Errors), problem.GetType().GetProperties().Select(p => p.Name));
        Assert.Contains("EndLodgeId", problem.Errors.Keys);
    }

    [Fact]
    public async Task PostStage_WithZeroLodgeIds_ReturnsValidationProblem()
    {
        var response = await Client.PostAsJsonAsync("/api/stages", new
        {
            startLodgeId = 0,
            endLodgeId = 0,
            durationMinutes = 60,
            distanceMeters = 3000,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadJsonAsync<ValidationProblemDetails>(response);
        Assert.Contains("StartLodgeId", problem.Errors.Keys);
        Assert.Contains("EndLodgeId", problem.Errors.Keys);
    }

    [Fact]
    public async Task PostStage_WithMissingLodge_ReturnsBadRequestProblemDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/stages", new
        {
            startLodgeId = 100,
            endLodgeId = 200,
            durationMinutes = 60,
            distanceMeters = 3000,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadJsonAsync<ProblemDetails>(response);
        Assert.Equal(400, problem.Status);
        Assert.Contains("does not exist", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostStage_WithDuplicateDirection_ReturnsConflictProblemDetails()
    {
        var (start, end) = await SeedTwoLodgesAsync();

        var payload = new
        {
            startLodgeId = start.Id,
            endLodgeId = end.Id,
            durationMinutes = 60,
            distanceMeters = 3000,
        };

        var first = await Client.PostAsJsonAsync("/api/stages", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await Client.PostAsJsonAsync("/api/stages", payload);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var problem = await ReadJsonAsync<ProblemDetails>(duplicate);
        Assert.Equal(409, problem.Status);
        Assert.Contains("same start and end lodges", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostStage_AllowsReversedStageForMvp()
    {
        var (start, end) = await SeedTwoLodgesAsync();

        var forward = await Client.PostAsJsonAsync("/api/stages", new
        {
            startLodgeId = start.Id,
            endLodgeId = end.Id,
            durationMinutes = 60,
            distanceMeters = 3000,
        });

        var reverse = await Client.PostAsJsonAsync("/api/stages", new
        {
            startLodgeId = end.Id,
            endLodgeId = start.Id,
            durationMinutes = 75,
            distanceMeters = 3000,
        });

        Assert.Equal(HttpStatusCode.Created, forward.StatusCode);
        Assert.Equal(HttpStatusCode.Created, reverse.StatusCode);
    }

    [Fact]
    public async Task GetStage_ReturnsResponseDtoWithNestedLodgeSummaries()
    {
        var (start, end) = await SeedTwoLodgesAsync();

        var create = await Client.PostAsJsonAsync("/api/stages", new
        {
            startLodgeId = start.Id,
            endLodgeId = end.Id,
            durationMinutes = 90,
            distanceMeters = 4200,
        });

        using var createdDocument = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = createdDocument.RootElement.GetProperty("id").GetInt64();

        var response = await Client.GetAsync($"/api/stages/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(id, root.GetProperty("id").GetInt64());
        Assert.Equal("Rifugio Auronzo", root.GetProperty("startLodge").GetProperty("name").GetString());
        Assert.Equal("Rifugio Lavaredo", root.GetProperty("endLodge").GetProperty("name").GetString());
        Assert.True(root.TryGetProperty("createdAt", out _));
    }
}
```

- [ ] **Step 2: Run stage tests to verify they fail**

Run:

```bash
dotnet test apps/api.Tests/api.Tests.csproj --filter FullyQualifiedName~StagesApiTests
```

Expected: at least one test fails because same-lodge validation, positive lodge ID validation, `ProblemDetails` constraint responses, and `DateTimeOffset` response typing are not fully implemented yet.

---

### Task 6: Implement Stage DTO Validation And ProblemDetails Errors

**Files:**
- Modify: `apps/api/Dtos/StageDtos.cs`
- Modify: `apps/api/Controllers/StagesController.cs`

- [ ] **Step 1: Replace Stage DTOs**

Replace `apps/api/Dtos/StageDtos.cs` with:

```csharp
namespace Api.Dtos;

using System.ComponentModel.DataAnnotations;

/// <summary>Payload for creating or updating a stage.</summary>
public sealed record StageRequestDto
{
    [Range(1, long.MaxValue)]
    public required long StartLodgeId { get; init; }

    [Range(1, long.MaxValue)]
    public required long EndLodgeId { get; init; }

    [Range(1, int.MaxValue)]
    public required int DurationMinutes { get; init; }

    [Range(1, int.MaxValue)]
    public required int DistanceMeters { get; init; }
}

/// <summary>Represents a stage returned by the API.</summary>
public sealed record StageResponseDto(
    long Id,
    LodgeSummaryDto StartLodge,
    LodgeSummaryDto EndLodge,
    int DurationMinutes,
    int DistanceMeters,
    DateTimeOffset CreatedAt);
```

- [ ] **Step 2: Update StagesController validation and query behavior**

In `apps/api/Controllers/StagesController.cs`:

1. In `GetStages`, add `AsNoTracking()` before `OrderBy`.
2. In `StageResponseQuery`, add `AsNoTracking()` before `Select`.
3. At the start of `PutStage` and `PostStage`, before querying or creating entities, add:

```csharp
if (request.StartLodgeId == request.EndLodgeId)
{
    ModelState.AddModelError(nameof(StageRequestDto.EndLodgeId), "End lodge must be different from start lodge.");
    return ValidationProblem(ModelState);
}
```

4. Remove this line from `PutStage`:

```csharp
_context.Entry(stage).State = EntityState.Modified;
```

- [ ] **Step 3: Replace database exception responses**

Replace `HandleDatabaseException` in `apps/api/Controllers/StagesController.cs` with:

```csharp
private ActionResult HandleDatabaseException(PostgresException exception)
{
    return exception.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Stage already exists.",
            detail: "A stage with the same start and end lodges already exists."),
        PostgresErrorCodes.ForeignKeyViolation => Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid lodge reference.",
            detail: "Start or end lodge does not exist."),
        _ => throw exception,
    };
}
```

- [ ] **Step 4: Run stage tests**

Run:

```bash
dotnet test apps/api.Tests/api.Tests.csproj --filter FullyQualifiedName~StagesApiTests
```

Expected: all `StagesApiTests` pass.

- [ ] **Step 5: Commit**

```bash
git add apps/api/Dtos/StageDtos.cs apps/api/Controllers/StagesController.cs apps/api.Tests/StagesApiTests.cs
git commit -m "feat: validate stages and return problem details"
```

---

### Task 7: Convert API Timestamp Types To DateTimeOffset

**Files:**
- Modify: `apps/api/Models/Lodge.cs`
- Modify: `apps/api/Models/Stage.cs`
- Create: `apps/api/Migrations/<GeneratedMigration>.cs`
- Modify: `apps/api/Migrations/AppDbContextModelSnapshot.cs`

- [ ] **Step 1: Change persistence timestamp types**

In `apps/api/Models/Lodge.cs`, change:

```csharp
public DateTime CreatedAt { get; set; }
```

to:

```csharp
public DateTimeOffset CreatedAt { get; set; }
```

In `apps/api/Models/Stage.cs`, change:

```csharp
public DateTime CreatedAt { get; set; }
```

to:

```csharp
public DateTimeOffset CreatedAt { get; set; }
```

- [ ] **Step 2: Add EF migration**

Run:

```bash
dotnet ef migrations add CreatedAtDateTimeOffset --project apps/api/api.csproj
```

Expected: a migration is created under `apps/api/Migrations/`. Review it. Because PostgreSQL `timestamp with time zone` maps naturally to offset-aware values through Npgsql, the migration may be empty or metadata-only. Keep the migration if the model snapshot changes.

- [ ] **Step 3: Run full API tests**

Run:

```bash
dotnet test apps/api.Tests/api.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 4: Commit**

```bash
git add apps/api/Models/Lodge.cs apps/api/Models/Stage.cs apps/api/Migrations
git commit -m "feat: expose timestamps as date time offsets"
```

---

### Task 8: Clean Up API Package References

**Files:**
- Modify: `apps/api/api.csproj`

- [ ] **Step 1: Remove packages that are not needed by the API**

In `apps/api/api.csproj`, remove these package references unless a local check proves they are required:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.9" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.9" />
<PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="10.0.2" />
```

Keep:

```xml
<PackageReference Include="EFCore.NamingConventions" Version="10.0.1" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.9" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.9">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.9">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.2" />
<PackageReference Include="NSwag.AspNetCore" Version="14.7.1" />
```

- [ ] **Step 2: Build API and tests**

Run:

```bash
dotnet build apps/api/api.csproj
dotnet build apps/api.Tests/api.Tests.csproj
```

Expected: both builds succeed. The previous `NuGet.Packaging` / `NuGet.Protocol` warnings should be reduced or gone if they came from the removed tooling package chain.

- [ ] **Step 3: Run full tests**

Run:

```bash
dotnet test apps/api.Tests/api.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 4: Commit**

```bash
git add apps/api/api.csproj
git commit -m "chore: remove unused api packages"
```

---

### Task 9: Final Verification And API Smoke Checks

**Files:**
- Modify: `apps/api/api.http`

- [ ] **Step 1: Update HTTP scratch file**

Replace `apps/api/api.http` with:

```http
@api_HostAddress = http://localhost:5108

GET {{api_HostAddress}}/api/lodges
Accept: application/json

###

POST {{api_HostAddress}}/api/lodges
Content-Type: application/json

{
  "name": "Rifugio Auronzo"
}

###

POST {{api_HostAddress}}/api/stages
Content-Type: application/json

{
  "startLodgeId": 1,
  "endLodgeId": 2,
  "durationMinutes": 90,
  "distanceMeters": 4200
}
```

- [ ] **Step 2: Run database-backed final verification**

Run:

```bash
dotnet build apps/api/api.csproj
dotnet test apps/api.Tests/api.Tests.csproj
```

Expected: build succeeds and all tests pass.

- [ ] **Step 3: Optionally verify OpenAPI locally**

Run database if needed:

```bash
docker compose up -d
```

Run API:

```bash
dotnet run --project apps/api/api.csproj --no-launch-profile
```

Open:

```text
http://localhost:5108/openapi/v1.json
```

Expected:

- Lodge request schemas contain only `name`.
- Lodge response schema contains `id`, `name`, and `createdAt`.
- Stage request schema contains `startLodgeId`, `endLodgeId`, `durationMinutes`, and `distanceMeters`.
- No API schema exposes `Api.Models.Lodge` or `Api.Models.Stage` as request/response contracts.

- [ ] **Step 4: Commit**

```bash
git add apps/api/api.http
git commit -m "docs: update api request examples"
```

---

## Self-Review Checklist

- DTO-only API contract is implemented for lodges and stages.
- Server-owned fields are not accepted in request DTOs.
- `CreatedAt` is exposed as `DateTimeOffset` in response DTOs.
- Existing `Stage` MVP model remains intact.
- Reversed stages are allowed.
- Exact duplicate direction stages return `409 ProblemDetails`.
- Missing lodge references return `400 ProblemDetails`.
- Same-lodge stages return `400 ValidationProblemDetails`.
- Referenced-lodge deletes return `409 ProblemDetails`.
- Read-only EF queries use `AsNoTracking()`.
- Test project uses PostgreSQL Testcontainers.
- Final verification includes `dotnet build` and `dotnet test`.

## Execution Notes

- Run Testcontainers tests in an environment with Docker available.
- If `dotnet ef` is not installed as a local/global tool, install or restore it before Task 7.
- Do not use EF InMemory for these tests; it does not enforce the PostgreSQL behavior this plan is designed to protect.
- Keep commits small and aligned with the tasks above.
