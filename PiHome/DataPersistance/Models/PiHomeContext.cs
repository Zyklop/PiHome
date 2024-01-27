using System;
using System.Collections.Generic;
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

        public virtual DbSet<Button> Buttons { get; set; } = null!;
        public virtual DbSet<ButtonMapping> ButtonMappings { get; set; } = null!;
        public virtual DbSet<Feature> Features { get; set; } = null!;
        public virtual DbSet<Led> Leds { get; set; } = null!;
        public virtual DbSet<LedPreset> LedPresets { get; set; } = null!;
        public virtual DbSet<LedPresetValue> LedPresetValues { get; set; } = null!;
        public virtual DbSet<Log> Logs { get; set; } = null!;
        public virtual DbSet<LogConfiguration> LogConfigurations { get; set; } = null!;
        public virtual DbSet<Module> Modules { get; set; } = null!;
        public virtual DbSet<PresetActivation> PresetActivations { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseNpgsql("Host=localhost;Database=PiHome;Username=writer;Password=PiWriter");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Button>(entity =>
            {
                entity.ToTable("Button");

                entity.HasIndex(e => e.ToggleGroup, "Button_ToggleGroup");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.LastActivation).HasColumnType("timestamp(0) without time zone");

                entity.Property(e => e.Name).HasMaxLength(20);
            });

            modelBuilder.Entity<ButtonMapping>(entity =>
            {
                entity.HasKey(e => new { e.ButtonId, e.ActionId });

                entity.ToTable("ButtonMapping");

                entity.Property(e => e.Description).HasMaxLength(20);

                entity.HasOne(d => d.Button)
                    .WithMany(p => p.ButtonMappings)
                    .HasForeignKey(d => d.ButtonId)
                    .HasConstraintName("FK_ButtonMapping-Button");

                entity.HasOne(d => d.ToggleOffPreset)
                    .WithMany()
                    .HasForeignKey(d => d.ToggleOffPresetId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_ButtonMapping-PresetOff");

                entity.HasOne(d => d.ToggleOnPreset)
                    .WithMany()
                    .HasForeignKey(d => d.ToggleOnPresetId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_ButtonMapping-PresetOn");
            });

            modelBuilder.Entity<Feature>(entity =>
            {
                entity.ToTable("Feature");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Name).HasMaxLength(30);

                entity.Property(e => e.Unit).HasMaxLength(20);
            });

            modelBuilder.Entity<Led>(entity =>
            {
                entity.ToTable("Led");

                entity.HasIndex(e => new { e.Index, e.ModuleId }, "UC_Key")
                    .IsUnique();

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.Leds)
                    .HasForeignKey(d => d.ModuleId)
                    .HasConstraintName("FK_Module");
            });

            modelBuilder.Entity<LedPreset>(entity =>
            {
                entity.ToTable("LedPreset");

                entity.HasIndex(e => e.Name, "IX_Name");

                entity.HasIndex(e => e.Name, "UC_Name")
                    .IsUnique();

                entity.Property(e => e.ChangeDate).HasColumnType("timestamp without time zone");

                entity.Property(e => e.Name).HasMaxLength(30);
            });

            modelBuilder.Entity<LedPresetValue>(entity =>
            {
                entity.HasIndex(e => e.PresetId, "IX_Preset");

                entity.HasIndex(e => new { e.LedId, e.PresetId }, "UC_PresetId_LedId")
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
                entity.ToTable("Log");

                entity.HasIndex(e => new { e.Time, e.LogConfigurationId }, "IX_Time")
                    .HasNullSortOrder(new[] { NullSortOrder.NullsLast, NullSortOrder.NullsLast })
                    .HasSortOrder(new[] { SortOrder.Descending, SortOrder.Ascending });

                entity.Property(e => e.Time).HasColumnType("timestamp without time zone");

                entity.HasOne(d => d.LogConfiguration)
                    .WithMany(p => p.Logs)
                    .HasForeignKey(d => d.LogConfigurationId)
                    .HasConstraintName("FK_LogConfiguration");
            });

            modelBuilder.Entity<LogConfiguration>(entity =>
            {
                entity.ToTable("LogConfiguration");

                entity.HasIndex(e => new { e.ModuleId, e.FeatureId }, "IX_Module_Feature");

                entity.HasIndex(e => new { e.ModuleId, e.FeatureId }, "UC_LogSetting")
                    .IsUnique();

                entity.Property(e => e.NextPoll).HasColumnType("timestamp without time zone");

                entity.HasOne(d => d.Feature)
                    .WithMany(p => p.LogConfigurations)
                    .HasForeignKey(d => d.FeatureId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Feature");

                entity.HasOne(d => d.Module)
                    .WithMany(p => p.LogConfigurations)
                    .HasForeignKey(d => d.ModuleId)
                    .HasConstraintName("FK_Module");
            });

            modelBuilder.Entity<Module>(entity =>
            {
                entity.ToTable("Module");

                entity.HasIndex(e => e.Ip, "UC_Ip")
                    .IsUnique();

                entity.HasIndex(e => e.Name, "UC_Module_Name")
                    .IsUnique();

                entity.Property(e => e.Name).HasMaxLength(30);
            });

            modelBuilder.Entity<PresetActivation>(entity =>
            {
                entity.ToTable("PresetActivation");

                entity.Property(e => e.Active).HasColumnType("bit(1)");

                entity.Property(e => e.DaysOfWeek).HasColumnType("bit(7)[]");

                entity.Property(e => e.NextActivationTime).HasColumnType("timestamp without time zone");

                entity.HasOne(d => d.Preset)
                    .WithMany(p => p.PresetActivations)
                    .HasForeignKey(d => d.PresetId)
                    .HasConstraintName("FK_PresetActivation");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
