using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DataPersistance.Models
{
    public partial class PiHomeContext : DbContext
    {
        public PiHomeContext()
        {
        }

        public PiHomeContext(DbContextOptions<PiHomeContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Feature> Feature { get; set; }
        public virtual DbSet<Led> Led { get; set; }
        public virtual DbSet<LedPreset> LedPreset { get; set; }
        public virtual DbSet<LedPresetValues> LedPresetValues { get; set; }
        public virtual DbSet<Log> Log { get; set; }
        public virtual DbSet<LogConfiguration> LogConfiguration { get; set; }
        public virtual DbSet<Module> Module { get; set; }
        public virtual DbSet<PresetActivation> PresetActivation { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseNpgsql("Host=localhost;Database=PiHome;Username=writer;Password=PiWriter");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Feature>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.Unit)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<Led>(entity =>
            {
                entity.HasIndex(e => new { e.Index, e.ModuleId })
                    .HasName("UC_Key")
                    .IsUnique();

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.Led)
                    .HasForeignKey(d => d.ModuleId)
                    .HasConstraintName("FK_Module");
            });

            modelBuilder.Entity<LedPreset>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("IX_Name");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(30);
            });

            modelBuilder.Entity<LedPresetValues>(entity =>
            {
                entity.HasIndex(e => e.PresetId)
                    .HasName("IX_Preset");

                entity.HasIndex(e => new { e.LedId, e.PresetId })
                    .HasName("UC_PresetId_LedId")
                    .IsUnique();

                entity.HasOne(d => d.Led)
                    .WithMany(p => p.LedPresetValues)
                    .HasForeignKey(d => d.LedId)
                    .HasConstraintName("FK_Led");

                entity.HasOne(d => d.Preset)
                    .WithMany(p => p.LedPresetValues)
                    .HasForeignKey(d => d.PresetId)
                    .HasConstraintName("FK_Preset");
            });

            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasIndex(e => new { e.Time, e.LogConfigurationId })
                    .HasName("IX_Time");

                entity.HasOne(d => d.LogConfiguration)
                    .WithMany(p => p.Log)
                    .HasForeignKey(d => d.LogConfigurationId)
                    .HasConstraintName("FK_LogConfiguration");
            });

            modelBuilder.Entity<LogConfiguration>(entity =>
            {
                entity.HasIndex(e => new { e.ModuleId, e.FeatureId })
                    .HasName("IX_Module_Feature");

                entity.HasOne(d => d.Feature)
                    .WithMany(p => p.LogConfiguration)
                    .HasForeignKey(d => d.FeatureId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Feature");

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.LogConfiguration)
                    .HasForeignKey(d => d.ModuleId)
                    .HasConstraintName("FK_Module");
            });

            modelBuilder.Entity<Module>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("UC_Module_Name")
                    .IsUnique();

                entity.Property(e => e.FeatureIds).IsRequired();

                entity.Property(e => e.Ip).IsRequired();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(30);
            });

            modelBuilder.Entity<PresetActivation>(entity =>
            {
                entity.Property(e => e.ActivationTime).HasColumnType("time(6) without time zone");

                entity.Property(e => e.Active)
                    .IsRequired()
                    .HasColumnType("bit(1)");

                entity.Property(e => e.DaysOfWeek)
                    .IsRequired()
                    .HasColumnType("bit(7)[]");

                entity.HasOne(d => d.Preset)
                    .WithMany(p => p.PresetActivation)
                    .HasForeignKey(d => d.PresetId)
                    .HasConstraintName("FK_PresetActivation");
            });
        }
    }
}
