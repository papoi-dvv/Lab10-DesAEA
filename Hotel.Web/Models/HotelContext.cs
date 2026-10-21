using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Models;

public partial class HotelContext : DbContext
{
    public HotelContext()
    {
    }

    public HotelContext(DbContextOptions<HotelContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Habitacion> Habitacions { get; set; }

    public virtual DbSet<Huesped> Huespeds { get; set; }

    public virtual DbSet<Reserva> Reservas { get; set; }

    public virtual DbSet<TipoHabitacion> TipoHabitacions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Habitacion>(entity =>
        {
            entity.ToTable("Habitacion");

            entity.HasIndex(e => e.Numero, "UQ_Habitacion_Numero").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Habitacion_Activo");
            entity.Property(e => e.Numero).HasMaxLength(10);

            entity.HasOne(d => d.TipoHabitacion).WithMany(p => p.Habitacions)
                .HasForeignKey(d => d.TipoHabitacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Habitacion_TipoHabitacion");
        });

        modelBuilder.Entity<Huesped>(entity =>
        {
            entity.ToTable("Huesped");

            entity.HasIndex(e => e.Dni, "UQ_Huesped_Dni").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Huesped_Activo");
            entity.Property(e => e.Apellidos).HasMaxLength(80);
            entity.Property(e => e.Dni)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Email).HasMaxLength(120);
            entity.Property(e => e.Nombres).HasMaxLength(80);
            entity.Property(e => e.Telefono).HasMaxLength(20);
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.ToTable("Reserva");

            entity.HasIndex(e => new { e.HabitacionId, e.FechaIngreso, e.FechaSalida }, "IX_Reserva_Habitacion_Fechas");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .HasDefaultValue("Pendiente", "DF_Reserva_Estado");
            entity.Property(e => e.FechaRegistro)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_Reserva_FechaRegistro");
            entity.Property(e => e.Observaciones).HasMaxLength(200);

            entity.HasOne(d => d.Habitacion).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.HabitacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reserva_Habitacion");

            entity.HasOne(d => d.Huesped).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.HuespedId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reserva_Huesped");
        });

        modelBuilder.Entity<TipoHabitacion>(entity =>
        {
            entity.ToTable("TipoHabitacion");

            entity.HasIndex(e => e.Nombre, "UQ_TipoHabitacion_Nombre").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(50);
            entity.Property(e => e.PrecioNoche).HasColumnType("decimal(10, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
