using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DietWorker.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MealHistory> MealHistories { get; set; }

    public virtual DbSet<PastiNonConsentiti> PastiNonConsentitis { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MealHistory>(entity =>
        {
            entity.ToTable("MealHistory");

            entity.Property(e => e.VarietyScore).HasDefaultValue(0);
        });

        modelBuilder.Entity<PastiNonConsentiti>(entity =>
        {
            entity.ToTable("PastiNonConsentiti");

            entity.HasIndex(e => e.Name, "idx_pastinonconsentiti_name");

            entity.Property(e => e.AddedOn).HasDefaultValueSql("date('now')");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
