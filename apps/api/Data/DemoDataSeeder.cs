using System.Text.Json;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Data;

public static class DemoDataSeeder
{
    private const string SeedDataDirectoryName = "SeedData";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void Seed(DbContext context, bool _)
    {
        if (context is not AppDbContext appContext)
        {
            return;
        }

        ReloadPostgresTypes(appContext);

        var seedData = LoadSeedData();

        foreach (var lodgeSeed in seedData.Lodges)
        {
            UpsertLodge(appContext, lodgeSeed);
        }

        appContext.SaveChanges();

        foreach (var stageSeed in seedData.Stages)
        {
            UpsertStage(appContext, stageSeed);
        }

        appContext.SaveChanges();

        foreach (var tourSeed in seedData.Tours)
        {
            UpsertTour(appContext, tourSeed);
        }

        appContext.SaveChanges();

        foreach (var variantSeed in seedData.TourVariants)
        {
            UpsertTourVariant(appContext, variantSeed);
        }

        appContext.SaveChanges();

        foreach (var tourVariantStageSeed in seedData.TourVariantStages)
        {
            UpsertTourVariantStage(appContext, tourVariantStageSeed);
        }

        appContext.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, bool _, CancellationToken cancellationToken)
    {
        if (context is not AppDbContext appContext)
        {
            return;
        }

        await ReloadPostgresTypesAsync(appContext, cancellationToken);

        var seedData = LoadSeedData();

        foreach (var lodgeSeed in seedData.Lodges)
        {
            await UpsertLodgeAsync(appContext, lodgeSeed, cancellationToken);
        }

        await appContext.SaveChangesAsync(cancellationToken);

        foreach (var stageSeed in seedData.Stages)
        {
            await UpsertStageAsync(appContext, stageSeed, cancellationToken);
        }

        await appContext.SaveChangesAsync(cancellationToken);

        foreach (var tourSeed in seedData.Tours)
        {
            await UpsertTourAsync(appContext, tourSeed, cancellationToken);
        }

        await appContext.SaveChangesAsync(cancellationToken);

        foreach (var variantSeed in seedData.TourVariants)
        {
            await UpsertTourVariantAsync(appContext, variantSeed, cancellationToken);
        }

        await appContext.SaveChangesAsync(cancellationToken);

        foreach (var tourVariantStageSeed in seedData.TourVariantStages)
        {
            await UpsertTourVariantStageAsync(appContext, tourVariantStageSeed, cancellationToken);
        }

        await appContext.SaveChangesAsync(cancellationToken);
    }

    private static void ReloadPostgresTypes(AppDbContext context)
    {
        if (context.Database.GetDbConnection() is NpgsqlConnection connection)
        {
            connection.ReloadTypes();
        }
    }

    private static async Task ReloadPostgresTypesAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        if (context.Database.GetDbConnection() is NpgsqlConnection connection)
        {
            await connection.ReloadTypesAsync(cancellationToken);
        }
    }

    private static void UpsertLodge(AppDbContext context, LodgeSeed seed)
    {
        var lodge = context.Lodges.SingleOrDefault(l => l.Name == seed.Name);

        if (lodge is null)
        {
            context.Lodges.Add(new Lodge
            {
                Name = seed.Name,
                Description = seed.Description,
                CountryCode = seed.CountryCode
            });
            return;
        }

        lodge.Description = seed.Description;
        lodge.CountryCode = seed.CountryCode;
    }

    private static async Task UpsertLodgeAsync(
        AppDbContext context,
        LodgeSeed seed,
        CancellationToken cancellationToken)
    {
        var lodge = await context.Lodges.SingleOrDefaultAsync(l => l.Name == seed.Name, cancellationToken);

        if (lodge is null)
        {
            context.Lodges.Add(new Lodge
            {
                Name = seed.Name,
                Description = seed.Description,
                CountryCode = seed.CountryCode
            });
            return;
        }

        lodge.Description = seed.Description;
        lodge.CountryCode = seed.CountryCode;
    }

    private static void UpsertStage(AppDbContext context, StageSeed seed)
    {
        var startLodge = GetRequiredLodge(context, seed.StartLodgeName);
        var endLodge = GetRequiredLodge(context, seed.EndLodgeName);
        var stage = context.Stages.SingleOrDefault(
            s => s.StartLodgeId == startLodge.Id && s.EndLodgeId == endLodge.Id);

        if (stage is null)
        {
            context.Stages.Add(new Stage
            {
                StartLodgeId = startLodge.Id,
                EndLodgeId = endLodge.Id,
                DurationMinutes = seed.DurationMinutes,
                DistanceMeters = seed.DistanceMeters
            });
            return;
        }

        stage.DurationMinutes = seed.DurationMinutes;
        stage.DistanceMeters = seed.DistanceMeters;
    }

    private static async Task UpsertStageAsync(
        AppDbContext context,
        StageSeed seed,
        CancellationToken cancellationToken)
    {
        var startLodge = await GetRequiredLodgeAsync(context, seed.StartLodgeName, cancellationToken);
        var endLodge = await GetRequiredLodgeAsync(context, seed.EndLodgeName, cancellationToken);
        var stage = await context.Stages.SingleOrDefaultAsync(
            s => s.StartLodgeId == startLodge.Id && s.EndLodgeId == endLodge.Id,
            cancellationToken);

        if (stage is null)
        {
            context.Stages.Add(new Stage
            {
                StartLodgeId = startLodge.Id,
                EndLodgeId = endLodge.Id,
                DurationMinutes = seed.DurationMinutes,
                DistanceMeters = seed.DistanceMeters
            });
            return;
        }

        stage.DurationMinutes = seed.DurationMinutes;
        stage.DistanceMeters = seed.DistanceMeters;
    }

    private static void UpsertTour(AppDbContext context, TourSeed seed)
    {
        var tour = context.Tours.SingleOrDefault(t => t.Name == seed.Name);

        if (tour is null)
        {
            context.Tours.Add(new Tour
            {
                Name = seed.Name,
                Description = seed.Description
            });
            return;
        }

        tour.Description = seed.Description;
    }

    private static async Task UpsertTourAsync(
        AppDbContext context,
        TourSeed seed,
        CancellationToken cancellationToken)
    {
        var tour = await context.Tours.SingleOrDefaultAsync(t => t.Name == seed.Name, cancellationToken);

        if (tour is null)
        {
            context.Tours.Add(new Tour
            {
                Name = seed.Name,
                Description = seed.Description
            });
            return;
        }

        tour.Description = seed.Description;
    }

    private static void UpsertTourVariant(AppDbContext context, TourVariantSeed seed)
    {
        var tour = GetRequiredTour(context, seed.TourName);
        var variant = context.TourVariants.SingleOrDefault(
            v => v.TourId == tour.Id && v.Name == seed.Name);

        if (variant is null)
        {
            context.TourVariants.Add(new TourVariant
            {
                TourId = tour.Id,
                Name = seed.Name,
                Description = seed.Description,
                IsPrimary = seed.IsPrimary
            });
            return;
        }

        variant.Description = seed.Description;
        variant.IsPrimary = seed.IsPrimary;
    }

    private static async Task UpsertTourVariantAsync(
        AppDbContext context,
        TourVariantSeed seed,
        CancellationToken cancellationToken)
    {
        var tour = await GetRequiredTourAsync(context, seed.TourName, cancellationToken);
        var variant = await context.TourVariants.SingleOrDefaultAsync(
            v => v.TourId == tour.Id && v.Name == seed.Name,
            cancellationToken);

        if (variant is null)
        {
            context.TourVariants.Add(new TourVariant
            {
                TourId = tour.Id,
                Name = seed.Name,
                Description = seed.Description,
                IsPrimary = seed.IsPrimary
            });
            return;
        }

        variant.Description = seed.Description;
        variant.IsPrimary = seed.IsPrimary;
    }

    private static void UpsertTourVariantStage(AppDbContext context, TourVariantStageSeed seed)
    {
        var tour = GetRequiredTour(context, seed.TourName);
        var variant = GetRequiredTourVariant(context, tour.Id, seed.TourVariantName);
        var stage = GetRequiredStage(context, seed.StartLodgeName, seed.EndLodgeName);
        var tourVariantStage = context.TourVariantStages.SingleOrDefault(
            vs => vs.TourVariantId == variant.Id && vs.Order == seed.Order);

        if (tourVariantStage is null)
        {
            context.TourVariantStages.Add(new TourVariantStage
            {
                TourVariantId = variant.Id,
                StageId = stage.Id,
                Order = seed.Order
            });
            return;
        }

        tourVariantStage.StageId = stage.Id;
    }

    private static async Task UpsertTourVariantStageAsync(
        AppDbContext context,
        TourVariantStageSeed seed,
        CancellationToken cancellationToken)
    {
        var tour = await GetRequiredTourAsync(context, seed.TourName, cancellationToken);
        var variant = await GetRequiredTourVariantAsync(
            context,
            tour.Id,
            seed.TourVariantName,
            cancellationToken);
        var stage = await GetRequiredStageAsync(
            context,
            seed.StartLodgeName,
            seed.EndLodgeName,
            cancellationToken);
        var tourVariantStage = await context.TourVariantStages.SingleOrDefaultAsync(
            vs => vs.TourVariantId == variant.Id && vs.Order == seed.Order,
            cancellationToken);

        if (tourVariantStage is null)
        {
            context.TourVariantStages.Add(new TourVariantStage
            {
                TourVariantId = variant.Id,
                StageId = stage.Id,
                Order = seed.Order
            });
            return;
        }

        tourVariantStage.StageId = stage.Id;
    }

    private static Lodge GetRequiredLodge(AppDbContext context, string name)
    {
        return context.Lodges.SingleOrDefault(l => l.Name == name)
            ?? throw new InvalidOperationException($"Seed data references an unknown lodge: {name}.");
    }

    private static async Task<Lodge> GetRequiredLodgeAsync(
        AppDbContext context,
        string name,
        CancellationToken cancellationToken)
    {
        return await context.Lodges.SingleOrDefaultAsync(l => l.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"Seed data references an unknown lodge: {name}.");
    }

    private static Tour GetRequiredTour(AppDbContext context, string name)
    {
        return context.Tours.SingleOrDefault(t => t.Name == name)
            ?? throw new InvalidOperationException($"Seed data references an unknown tour: {name}.");
    }

    private static async Task<Tour> GetRequiredTourAsync(
        AppDbContext context,
        string name,
        CancellationToken cancellationToken)
    {
        return await context.Tours.SingleOrDefaultAsync(t => t.Name == name, cancellationToken)
            ?? throw new InvalidOperationException($"Seed data references an unknown tour: {name}.");
    }

    private static TourVariant GetRequiredTourVariant(AppDbContext context, long tourId, string name)
    {
        return context.TourVariants.SingleOrDefault(v => v.TourId == tourId && v.Name == name)
            ?? throw new InvalidOperationException($"Seed data references an unknown tour variant: {name}.");
    }

    private static async Task<TourVariant> GetRequiredTourVariantAsync(
        AppDbContext context,
        long tourId,
        string name,
        CancellationToken cancellationToken)
    {
        return await context.TourVariants.SingleOrDefaultAsync(
                v => v.TourId == tourId && v.Name == name,
                cancellationToken)
            ?? throw new InvalidOperationException($"Seed data references an unknown tour variant: {name}.");
    }

    private static Stage GetRequiredStage(AppDbContext context, string startLodgeName, string endLodgeName)
    {
        var startLodge = GetRequiredLodge(context, startLodgeName);
        var endLodge = GetRequiredLodge(context, endLodgeName);

        return context.Stages.SingleOrDefault(
                s => s.StartLodgeId == startLodge.Id && s.EndLodgeId == endLodge.Id)
            ?? throw new InvalidOperationException(
                $"Seed data references an unknown stage: {startLodgeName} to {endLodgeName}.");
    }

    private static async Task<Stage> GetRequiredStageAsync(
        AppDbContext context,
        string startLodgeName,
        string endLodgeName,
        CancellationToken cancellationToken)
    {
        var startLodge = await GetRequiredLodgeAsync(context, startLodgeName, cancellationToken);
        var endLodge = await GetRequiredLodgeAsync(context, endLodgeName, cancellationToken);

        return await context.Stages.SingleOrDefaultAsync(
                s => s.StartLodgeId == startLodge.Id && s.EndLodgeId == endLodge.Id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Seed data references an unknown stage: {startLodgeName} to {endLodgeName}.");
    }

    private static SeedData LoadSeedData()
    {
        var seedDataPath = ResolveSeedDataPath();

        return new SeedData(
            LoadSeedFile<LodgeSeed>(seedDataPath, "lodges.json"),
            LoadSeedFile<StageSeed>(seedDataPath, "stages.json"),
            LoadSeedFile<TourSeed>(seedDataPath, "tours.json"),
            LoadSeedFile<TourVariantSeed>(seedDataPath, "tourVariants.json"),
            LoadSeedFile<TourVariantStageSeed>(seedDataPath, "tourVariantStages.json"));
    }

    private static IReadOnlyList<T> LoadSeedFile<T>(string seedDataPath, string fileName)
    {
        var path = Path.Combine(seedDataPath, fileName);
        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<IReadOnlyList<T>>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Seed data file is empty: {path}.");
    }

    private static string ResolveSeedDataPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var seedDataPath = Path.Combine(directory.FullName, "Data", SeedDataDirectoryName);

            if (Directory.Exists(seedDataPath))
            {
                return seedDataPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find Data/{SeedDataDirectoryName} for demo seed data.");
    }

    private sealed record SeedData(
        IReadOnlyList<LodgeSeed> Lodges,
        IReadOnlyList<StageSeed> Stages,
        IReadOnlyList<TourSeed> Tours,
        IReadOnlyList<TourVariantSeed> TourVariants,
        IReadOnlyList<TourVariantStageSeed> TourVariantStages);

    private sealed record LodgeSeed(
        string Name,
        string Description,
        string CountryCode);

    private sealed record StageSeed(
        string StartLodgeName,
        string EndLodgeName,
        int DurationMinutes,
        int DistanceMeters);

    private sealed record TourSeed(
        string Name,
        string Description);

    private sealed record TourVariantSeed(
        string TourName,
        string Name,
        string Description,
        bool IsPrimary);

    private sealed record TourVariantStageSeed(
        string TourName,
        string TourVariantName,
        int Order,
        string StartLodgeName,
        string EndLodgeName);
}
