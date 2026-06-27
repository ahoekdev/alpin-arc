using Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Data;

public static class DemoDataSeeder
{
    private const string DefaultVariantName = "Default";

    private static readonly TourSeed[] Tours =
    [
        new(
            "Berliner Höhenweg",
            "A hut-to-hut traverse through the Zillertal Alps linking classic mountain huts on a demanding high route.",
            [
                new(
                    DefaultVariantName,
                    "The standard hut sequence across the Berliner Höhenweg.",
                    true,
                    [
                        new("Gamshütte", "Friesenberghaus", 540, 14500),
                        new("Friesenberghaus", "Olpererhütte", 150, 3900),
                        new("Olpererhütte", "Furtschaglhaus", 240, 9800),
                        new("Furtschaglhaus", "Berliner Hütte", 390, 11500),
                        new("Berliner Hütte", "Greizer Hütte", 420, 10000),
                        new("Greizer Hütte", "Kasseler Hütte", 540, 13000),
                        new("Kasseler Hütte", "Karl-von-Edel-Hütte", 540, 14000)
                    ])
            ]),
        new(
            "Stubaier Höhenweg",
            "A multi-day circuit through the Stubai Alps with sustained hut-to-hut walking and glacier views.",
            [
                new(
                    DefaultVariantName,
                    "The standard hut sequence across the Stubaier Höhenweg.",
                    true,
                    [
                        new("Starkenburger Hütte", "Franz-Senn-Hütte", 420, 15000),
                        new("Franz-Senn-Hütte", "Neue Regensburger Hütte", 300, 8500),
                        new("Neue Regensburger Hütte", "Dresdner Hütte", 420, 12000),
                        new("Dresdner Hütte", "Sulzenau Hütte", 180, 4500),
                        new("Sulzenau Hütte", "Nürnberger Hütte", 240, 6000),
                        new("Nürnberger Hütte", "Bremer Hütte", 360, 7500),
                        new("Bremer Hütte", "Innsbrucker Hütte", 420, 9000)
                    ])
            ]),
        new(
            "Sellrainer Hüttenrunde",
            "A compact hut circuit in the Sellrain mountains connecting high alpine huts above the Sellrain valley.",
            [
                new(
                    "Normal",
                    "The regular hut-to-hut line from the Potsdamer Hütte across Westfalenhaus and Pforzheimer Hütte to the Dortmunder Hütte.",
                    true,
                    [
                        new("Potsdamer Hütte", "Westfalenhaus", 300, 9000),
                        new("Westfalenhaus", "Pforzheimer Hütte", 360, 11000),
                        new("Pforzheimer Hütte", "Schweinfurter Hütte", 240, 9000),
                        new("Schweinfurter Hütte", "Dortmunder Hütte", 300, 12000)
                    ]),
                new(
                    "Alpine",
                    "A more alpine variant that links Westfalenhaus to Pforzheimer Hütte via the Winnebachseehütte.",
                    false,
                    [
                        new("Potsdamer Hütte", "Westfalenhaus", 300, 9000),
                        new("Westfalenhaus", "Winnebachseehütte", 360, 10000),
                        new("Winnebachseehütte", "Pforzheimer Hütte", 300, 9000),
                        new("Pforzheimer Hütte", "Schweinfurter Hütte", 240, 9000),
                        new("Schweinfurter Hütte", "Dortmunder Hütte", 300, 12000)
                    ])
            ])
    ];

    public static void Seed(DbContext context, bool _)
    {
        if (context is not AppDbContext appContext)
        {
            return;
        }

        ReloadPostgresTypes(appContext);

        foreach (var tourSeed in Tours)
        {
            SeedTour(appContext, tourSeed);
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

        foreach (var tourSeed in Tours)
        {
            await SeedTourAsync(appContext, tourSeed, cancellationToken);
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

    private static void SeedTour(AppDbContext context, TourSeed tourSeed)
    {
        var tour = context.Tours
            .Include(t => t.Variants)
            .SingleOrDefault(t => t.Name == tourSeed.Name);

        if (tour is null)
        {
            tour = new Tour { Name = tourSeed.Name, Description = tourSeed.Description };
            context.Tours.Add(tour);
            context.SaveChanges();
        }
        else if (tour.Description != tourSeed.Description)
        {
            tour.Description = tourSeed.Description;
            context.SaveChanges();
        }

        foreach (var variantSeed in tourSeed.Variants)
        {
            SeedTourVariant(context, tour, variantSeed);
        }
    }

    private static void SeedTourVariant(
        AppDbContext context,
        Tour tour,
        TourVariantSeed variantSeed)
    {
        var variant = context.TourVariants
            .SingleOrDefault(v => v.TourId == tour.Id && v.Name == variantSeed.Name);

        if (variant is null)
        {
            variant = new TourVariant
            {
                TourId = tour.Id,
                Name = variantSeed.Name,
                Description = variantSeed.Description,
                IsPrimary = variantSeed.IsPrimary
            };
            context.TourVariants.Add(variant);
            context.SaveChanges();
        }
        else if (variant.Description != variantSeed.Description || variant.IsPrimary != variantSeed.IsPrimary)
        {
            variant.Description = variantSeed.Description;
            variant.IsPrimary = variantSeed.IsPrimary;
            context.SaveChanges();
        }

        SeedStages(context, variant, variantSeed.Stages);
    }

    private static async Task SeedTourAsync(
        AppDbContext context,
        TourSeed tourSeed,
        CancellationToken cancellationToken)
    {
        var tour = await context.Tours
            .Include(t => t.Variants)
            .SingleOrDefaultAsync(t => t.Name == tourSeed.Name, cancellationToken);

        if (tour is null)
        {
            tour = new Tour { Name = tourSeed.Name, Description = tourSeed.Description };
            context.Tours.Add(tour);
            await context.SaveChangesAsync(cancellationToken);
        }
        else if (tour.Description != tourSeed.Description)
        {
            tour.Description = tourSeed.Description;
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (var variantSeed in tourSeed.Variants)
        {
            await SeedTourVariantAsync(context, tour, variantSeed, cancellationToken);
        }
    }

    private static async Task SeedTourVariantAsync(
        AppDbContext context,
        Tour tour,
        TourVariantSeed variantSeed,
        CancellationToken cancellationToken)
    {
        var variant = await context.TourVariants
            .SingleOrDefaultAsync(
                v => v.TourId == tour.Id && v.Name == variantSeed.Name,
                cancellationToken);

        if (variant is null)
        {
            variant = new TourVariant
            {
                TourId = tour.Id,
                Name = variantSeed.Name,
                Description = variantSeed.Description,
                IsPrimary = variantSeed.IsPrimary
            };
            context.TourVariants.Add(variant);
            await context.SaveChangesAsync(cancellationToken);
        }
        else if (variant.Description != variantSeed.Description || variant.IsPrimary != variantSeed.IsPrimary)
        {
            variant.Description = variantSeed.Description;
            variant.IsPrimary = variantSeed.IsPrimary;
            await context.SaveChangesAsync(cancellationToken);
        }

        await SeedStagesAsync(context, variant, variantSeed.Stages, cancellationToken);
    }

    private static void SeedStages(
        AppDbContext context,
        TourVariant variant,
        IReadOnlyList<StageSeed> stageSeeds)
    {
        for (var index = 0; index < stageSeeds.Count; index++)
        {
            var stageSeed = stageSeeds[index];
            var stage = GetOrCreateStage(context, stageSeed);
            var order = index + 1;

            var tourVariantStage = context.TourVariantStages
                .SingleOrDefault(vs => vs.TourVariantId == variant.Id && vs.Order == order);

            if (tourVariantStage is null)
            {
                context.TourVariantStages.Add(new TourVariantStage
                {
                    TourVariantId = variant.Id,
                    StageId = stage.Id,
                    Order = order
                });
            }
            else if (tourVariantStage.StageId != stage.Id)
            {
                tourVariantStage.StageId = stage.Id;
            }
        }
    }

    private static async Task SeedStagesAsync(
        AppDbContext context,
        TourVariant variant,
        IReadOnlyList<StageSeed> stageSeeds,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < stageSeeds.Count; index++)
        {
            var stageSeed = stageSeeds[index];
            var stage = await GetOrCreateStageAsync(context, stageSeed, cancellationToken);
            var order = index + 1;

            var tourVariantStage = await context.TourVariantStages.SingleOrDefaultAsync(
                    vs => vs.TourVariantId == variant.Id && vs.Order == order,
                    cancellationToken);

            if (tourVariantStage is null)
            {
                context.TourVariantStages.Add(new TourVariantStage
                {
                    TourVariantId = variant.Id,
                    StageId = stage.Id,
                    Order = order
                });
            }
            else if (tourVariantStage.StageId != stage.Id)
            {
                tourVariantStage.StageId = stage.Id;
            }
        }
    }

    private static Stage GetOrCreateStage(AppDbContext context, StageSeed stageSeed)
    {
        var startLodge = GetOrCreateLodge(context, stageSeed.StartLodgeName);
        var endLodge = GetOrCreateLodge(context, stageSeed.EndLodgeName);

        var stage = context.Stages.SingleOrDefault(
            s => s.StartLodgeId == startLodge.Id && s.EndLodgeId == endLodge.Id);

        if (stage is not null)
        {
            return stage;
        }

        stage = new Stage
        {
            StartLodgeId = startLodge.Id,
            EndLodgeId = endLodge.Id,
            DurationMinutes = stageSeed.DurationMinutes,
            DistanceMeters = stageSeed.DistanceMeters
        };

        context.Stages.Add(stage);
        context.SaveChanges();

        return stage;
    }

    private static async Task<Stage> GetOrCreateStageAsync(
        AppDbContext context,
        StageSeed stageSeed,
        CancellationToken cancellationToken)
    {
        var startLodge = await GetOrCreateLodgeAsync(context, stageSeed.StartLodgeName, cancellationToken);
        var endLodge = await GetOrCreateLodgeAsync(context, stageSeed.EndLodgeName, cancellationToken);

        var stage = await context.Stages.SingleOrDefaultAsync(
            s => s.StartLodgeId == startLodge.Id && s.EndLodgeId == endLodge.Id,
            cancellationToken);

        if (stage is not null)
        {
            return stage;
        }

        stage = new Stage
        {
            StartLodgeId = startLodge.Id,
            EndLodgeId = endLodge.Id,
            DurationMinutes = stageSeed.DurationMinutes,
            DistanceMeters = stageSeed.DistanceMeters
        };

        context.Stages.Add(stage);
        await context.SaveChangesAsync(cancellationToken);

        return stage;
    }

    private static Lodge GetOrCreateLodge(AppDbContext context, string name)
    {
        var lodge = context.Lodges.SingleOrDefault(l => l.Name == name);

        if (lodge is not null)
        {
            return lodge;
        }

        lodge = new Lodge { Name = name };
        context.Lodges.Add(lodge);
        context.SaveChanges();

        return lodge;
    }

    private static async Task<Lodge> GetOrCreateLodgeAsync(
        AppDbContext context,
        string name,
        CancellationToken cancellationToken)
    {
        var lodge = await context.Lodges.SingleOrDefaultAsync(l => l.Name == name, cancellationToken);

        if (lodge is not null)
        {
            return lodge;
        }

        lodge = new Lodge { Name = name };
        context.Lodges.Add(lodge);
        await context.SaveChangesAsync(cancellationToken);

        return lodge;
    }

    private sealed record TourSeed(
        string Name,
        string Description,
        IReadOnlyList<TourVariantSeed> Variants);

    private sealed record TourVariantSeed(
        string Name,
        string Description,
        bool IsPrimary,
        IReadOnlyList<StageSeed> Stages);

    private sealed record StageSeed(
        string StartLodgeName,
        string EndLodgeName,
        int DurationMinutes,
        int DistanceMeters);
}
