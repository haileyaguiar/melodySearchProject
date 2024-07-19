using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace melodySearchProject.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MeiFile> MeiFiles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=melodysearchmeidatabase.ct8ig60g4q3f.us-east-2.rds.amazonaws.com;Port=5432;Username=postgres;Password=ScouredElmContempt8;Database=meiDatabase");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeiFile>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("meiFiles_pkey");

            entity.ToTable("meiFiles");

            entity.Property(e => e.FileId).HasColumnName("file_id");
            entity.Property(e => e.FileContent)
                .HasColumnType("xml")
                .HasColumnName("file_content");
            entity.Property(e => e.FileName).HasColumnName("file_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
