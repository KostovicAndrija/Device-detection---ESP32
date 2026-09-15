using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceObservation> DeviceObservations => Set<DeviceObservation>();
    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<WhitelistEntry> WhitelistEntries => Set<WhitelistEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StaffDevice> StaffDevices => Set<StaffDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.HashId).HasColumnName("hash_id").HasMaxLength(256).IsRequired();
            entity.Property(x => x.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            entity.Property(x => x.FirstSeenAt).HasColumnName("first_seen_at").IsRequired();
            entity.Property(x => x.LastSeenAt).HasColumnName("last_seen_at").IsRequired();
            entity.HasIndex(x => x.HashId).IsUnique();
            entity.HasIndex(x => x.LastSeenAt);
        });

        modelBuilder.Entity<DeviceObservation>(entity =>
        {
            entity.ToTable("device_observations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
            entity.Property(x => x.SensorId).HasColumnName("sensor_id").HasMaxLength(64).IsRequired();
            entity.Property(x => x.SessionId).HasColumnName("session_id").HasMaxLength(128);
            entity.Property(x => x.SignalType).HasColumnName("signal_type").HasMaxLength(32).IsRequired();
            entity.Property(x => x.Rssi).HasColumnName("rssi").IsRequired();
            entity.Property(x => x.CapturedAt).HasColumnName("captured_at").IsRequired();
            entity.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(128);
            entity.HasIndex(x => x.CapturedAt);
            entity.HasIndex(x => new { x.SessionId, x.CapturedAt });
            entity.HasIndex(x => x.ExternalId).IsUnique();
        });

        modelBuilder.Entity<ExamSession>(entity =>
        {
            entity.ToTable("exam_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.RoomId).HasColumnName("room_id").HasMaxLength(64).IsRequired();
            entity.Property(x => x.StartsAt).HasColumnName("starts_at").IsRequired();
            entity.Property(x => x.EndsAt).HasColumnName("ends_at");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(x => x.RegistrationExpiresAt).HasColumnName("registration_expires_at");
            entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("alerts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DeviceId).HasColumnName("device_id").IsRequired();
            entity.Property(x => x.SessionId).HasColumnName("session_id").HasMaxLength(128);
            entity.Property(x => x.RiskScore).HasColumnName("risk_score").IsRequired();
            entity.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(300).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(x => x.AcknowledgedAt).HasColumnName("acknowledged_at");
            entity.HasIndex(x => new { x.SessionId, x.CreatedAt });
        });

        modelBuilder.Entity<WhitelistEntry>(entity =>
        {
            entity.ToTable("whitelist_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SessionId).HasColumnName("session_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.StudentRef).HasColumnName("student_ref").HasMaxLength(128).IsRequired();
            entity.Property(x => x.DeviceHash).HasColumnName("device_hash").HasMaxLength(256).IsRequired();
            entity.Property(x => x.ValidFrom).HasColumnName("valid_from").IsRequired();
            entity.Property(x => x.ValidTo).HasColumnName("valid_to");
            entity.Property(x => x.StaffDeviceId).HasColumnName("staff_device_id");
            entity.HasOne<StaffDevice>().WithMany().HasForeignKey(x => x.StaffDeviceId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.SessionId, x.DeviceHash });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Actor).HasColumnName("actor").HasMaxLength(128).IsRequired();
            entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(16).IsRequired();
            entity.Property(x => x.Resource).HasColumnName("resource").HasMaxLength(512).IsRequired();
            entity.Property(x => x.StatusCode).HasColumnName("status_code").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(128).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
            entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.StaffDeviceRevision).HasColumnName("staff_device_revision").IsConcurrencyToken();
        });

        modelBuilder.Entity<StaffDevice>(entity =>
        {
            entity.ToTable("staff_devices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Label).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DeviceHash).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.DeviceHash).IsUnique();
            entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ExamSession>().WithMany().HasForeignKey(x => x.RegistrationSessionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
        });
    }
}
