﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebServer.Models.AIoTDB;

public partial class AIoTDBContext : DbContext
{
    public AIoTDBContext(DbContextOptions<AIoTDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> User { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Chinese_Taiwan_Stroke_CI_AS");

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.ID).ValueGeneratedNever();
            entity.Property(e => e.Account)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.AccountNormalize)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Birthday).HasColumnType("datetime");
            entity.Property(e => e.CreatedDT).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.EmailNormalize)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.LockoutEnd).HasColumnType("datetime");
            entity.Property(e => e.Mobile).HasMaxLength(100);
            entity.Property(e => e.ModifiedDT).HasColumnType("datetime");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(256);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}