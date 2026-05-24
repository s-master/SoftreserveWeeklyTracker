using Microsoft.EntityFrameworkCore;
using SoftreserveTracker.Web.Models.Entities;

namespace SoftreserveTracker.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Roster> Rosters => Set<Roster>();
    public DbSet<RaidWeek> RaidWeeks => Set<RaidWeek>();
    public DbSet<RaidSession> RaidSessions => Set<RaidSession>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<SoftReserve> SoftReserves => Set<SoftReserve>();
    public DbSet<LootAward> LootAwards => Set<LootAward>();
    public DbSet<LootRoll> LootRolls => Set<LootRoll>();
    public DbSet<PlusOneBalance> PlusOneBalances => Set<PlusOneBalance>();
    public DbSet<SessionReservationResult> SessionReservationResults => Set<SessionReservationResult>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<KnownItem> KnownItems => Set<KnownItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Roster>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.AccessToken).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<RaidWeek>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RosterId, x.PeriodStart }).IsUnique();
            e.HasIndex(x => new { x.RosterId, x.WeekNumber }).IsUnique();
            e.HasOne(x => x.Roster).WithMany(x => x.RaidWeeks).HasForeignKey(x => x.RosterId);
        });

        modelBuilder.Entity<RaidSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RaidWeekId, x.RaidType, x.SessionDate, x.SoftresId });
            e.HasOne(x => x.RaidWeek).WithMany(x => x.Sessions).HasForeignKey(x => x.RaidWeekId);
        });

        modelBuilder.Entity<Player>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RosterId, x.NormalizedName }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.NormalizedName).HasMaxLength(100);
            e.HasOne(x => x.Roster).WithMany(x => x.Players).HasForeignKey(x => x.RosterId);
        });

        modelBuilder.Entity<SoftReserve>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RaidSessionId, x.PlayerId, x.ItemId });
            e.HasOne(x => x.RaidSession).WithMany(x => x.SoftReserves).HasForeignKey(x => x.RaidSessionId);
            e.HasOne(x => x.Player).WithMany(x => x.SoftReserves).HasForeignKey(x => x.PlayerId);
        });

        modelBuilder.Entity<LootAward>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.RaidSession).WithMany(x => x.LootAwards).HasForeignKey(x => x.RaidSessionId);
            e.HasOne(x => x.WinnerPlayer).WithMany(x => x.LootAwards).HasForeignKey(x => x.WinnerPlayerId);
        });

        modelBuilder.Entity<LootRoll>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.LootAwardId, x.PlayerId });
            e.Property(x => x.PlayerName).HasMaxLength(100);
            e.Property(x => x.PlayerClass).HasMaxLength(50);
            e.Property(x => x.Classification).HasMaxLength(20);
            e.HasOne(x => x.LootAward).WithMany(x => x.Rolls).HasForeignKey(x => x.LootAwardId);
            e.HasOne(x => x.Player).WithMany(x => x.LootRolls).HasForeignKey(x => x.PlayerId);
        });

        modelBuilder.Entity<PlusOneBalance>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PlayerId, x.ItemId }).IsUnique();
            e.HasOne(x => x.Player).WithMany(x => x.PlusOneBalances).HasForeignKey(x => x.PlayerId);
        });

        modelBuilder.Entity<SessionReservationResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RaidSessionId, x.PlayerId, x.ItemId });
            e.HasOne(x => x.RaidSession).WithMany(x => x.ReservationResults).HasForeignKey(x => x.RaidSessionId);
            e.HasOne(x => x.Player).WithMany(x => x.ReservationResults).HasForeignKey(x => x.PlayerId);
            e.HasOne(x => x.AwardedToPlayer).WithMany().HasForeignKey(x => x.AwardedToPlayerId);
        });

        modelBuilder.Entity<UploadedFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.RaidSession).WithMany(x => x.UploadedFiles).HasForeignKey(x => x.RaidSessionId);
        });

        modelBuilder.Entity<KnownItem>(e =>
        {
            e.HasKey(x => x.ItemId);
            e.Property(x => x.ItemId).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(200);
        });
    }
}
