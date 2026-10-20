using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MalapadCogonEConcern.Models.Entities;

namespace MalapadCogonEConcern.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Sitio> Sitios => Set<Sitio>();
    public DbSet<Concern> Concerns => Set<Concern>();
    public DbSet<ConcernAttachment> ConcernAttachments => Set<ConcernAttachment>();
    public DbSet<ConcernStatusHistory> ConcernStatusHistories => Set<ConcernStatusHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Concern Configurations
        builder.Entity<Concern>(entity =>
        {
            entity.HasIndex(c => c.ReferenceNumber)
                  .IsUnique();

            entity.HasOne(c => c.Category)
                  .WithMany(cat => cat.Concerns)
                  .HasForeignKey(c => c.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Sitio)
                  .WithMany(s => s.Concerns)
                  .HasForeignKey(c => c.SitioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.AssignedToUser)
                  .WithMany(u => u.AssignedConcerns)
                  .HasForeignKey(c => c.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Attachments
        builder.Entity<ConcernAttachment>(entity =>
        {
            entity.HasOne(a => a.Concern)
                  .WithMany(c => c.Attachments)
                  .HasForeignKey(a => a.ConcernId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.UploadedByUser)
                  .WithMany()
                  .HasForeignKey(a => a.UploadedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Status History
        builder.Entity<ConcernStatusHistory>(entity =>
        {
            entity.HasOne(h => h.Concern)
                  .WithMany(c => c.StatusHistories)
                  .HasForeignKey(h => h.ConcernId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.ChangedByUser)
                  .WithMany(u => u.StatusHistories)
                  .HasForeignKey(h => h.ChangedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Audit Log
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(l => l.User)
                  .WithMany()
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
