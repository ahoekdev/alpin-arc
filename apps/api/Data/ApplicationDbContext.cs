using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Lodge> Lodges => Set<Lodge>();
    public DbSet<LodgeFavorite> LodgeFavorites => Set<LodgeFavorite>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourVariant> TourVariants => Set<TourVariant>();
    public DbSet<TourVariantStage> TourVariantStages => Set<TourVariantStage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<Lodge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000).HasDefaultValue("");
            entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(2).IsFixedLength().HasDefaultValue("AT");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<LodgeFavorite>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LodgeId });
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.LodgeId);

            entity.HasOne(e => e.User)
                .WithMany(e => e.LodgeFavorites)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Lodge)
                .WithMany()
                .HasForeignKey(e => e.LodgeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Stage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.DurationMinutes).IsRequired();
            entity.Property(e => e.DistanceMeters).IsRequired();

            entity.HasIndex(e => new { e.StartLodgeId, e.EndLodgeId }).IsUnique();

            entity.HasOne(e => e.StartLodge)
                .WithMany()
                .HasForeignKey(e => e.StartLodgeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EndLodge)
                .WithMany()
                .HasForeignKey(e => e.EndLodgeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnType("citext");
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000).HasDefaultValue("");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<TourVariant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200).HasColumnType("citext");
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000).HasDefaultValue("");
            entity.Property(e => e.IsPrimary).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasIndex(e => new { e.TourId, e.Name }).IsUnique();

            entity.HasOne(e => e.Tour)
                .WithMany(e => e.Variants)
                .HasForeignKey(e => e.TourId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TourVariantStage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Order).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.ToTable(t => t.HasCheckConstraint("ck_tour_variant_stages_order_positive", "\"order\" >= 1"));

            entity.HasIndex(e => new { e.TourVariantId, e.Order }).IsUnique();
            entity.HasIndex(e => e.StageId);

            entity.HasOne(e => e.TourVariant)
                .WithMany(e => e.Stages)
                .HasForeignKey(e => e.TourVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Stage)
                .WithMany()
                .HasForeignKey(e => e.StageId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
