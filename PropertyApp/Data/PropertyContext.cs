using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Models;

namespace PropertyApp.Data;

public partial class PropertyContext : DbContext
{
    //public PropertyContext()
    //{
    //}

    public PropertyContext(DbContextOptions<PropertyContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Apartment> Apartments { get; set; }

    public virtual DbSet<ApartmentUser> ApartmentUsers { get; set; }

    public virtual DbSet<Measure> Measures { get; set; }

    public virtual DbSet<MeasureDevice> MeasureDevices { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAccess> UserAccesses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=Dbproperty");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Apartment>(entity =>
        {
            entity.HasKey(e => e.IdApartment).HasName("PK__Apartmen__2294CEAD06DAD377");

            entity.ToTable("Apartment");

            entity.Property(e => e.IdApartment).HasColumnName("id_apartment");
            entity.Property(e => e.IdProperty).HasColumnName("id_property");
            entity.Property(e => e.StaircaseDoor)
                .HasMaxLength(10)
                .HasColumnName("staircase_door");

            entity.HasOne(d => d.IdPropertyNavigation).WithMany(p => p.Apartments)
                .HasForeignKey(d => d.IdProperty)
                .HasConstraintName("FK_Apartment_Property");
        });

        modelBuilder.Entity<ApartmentUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Apartmen__3213E83F194D1543");

            entity.ToTable("Apartment_User");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.FromDate).HasColumnName("from_date");
            entity.Property(e => e.IdApartment).HasColumnName("id_apartment");
            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.UserRole)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("user_role");

            entity.HasOne(d => d.IdApartmentNavigation).WithMany(p => p.ApartmentUsers)
                .HasForeignKey(d => d.IdApartment)
                .HasConstraintName("FK_ApartmentUser_Apartment");

            entity.HasOne(d => d.IdUserNavigation).WithMany(p => p.ApartmentUsers)
                .HasForeignKey(d => d.IdUser)
                .HasConstraintName("FK_ApartmentUser_User");
        });

        modelBuilder.Entity<Measure>(entity =>
        {
            entity.HasKey(e => e.MeasureId).HasName("PK__Measure__38DE4810034E66A1");

            entity.ToTable("Measure");

            entity.Property(e => e.MeasureId).HasColumnName("measure_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 4)")
                .HasColumnName("amount");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.IdMeasureDevice).HasColumnName("id_measure_device");
            entity.Property(e => e.IdUser).HasColumnName("id_user");

            entity.HasOne(d => d.IdMeasureDeviceNavigation).WithMany(p => p.Measures)
                .HasForeignKey(d => d.IdMeasureDevice)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Measure_Device");

            entity.HasOne(d => d.IdUserNavigation).WithMany(p => p.Measures)
                .HasForeignKey(d => d.IdUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Measure_User");
        });

        modelBuilder.Entity<MeasureDevice>(entity =>
        {
            entity.HasKey(e => e.IdMeasureDevice).HasName("PK__Measured__B2700B59081C682D");

            entity.ToTable("Measuredevice");

            entity.Property(e => e.IdMeasureDevice).HasColumnName("id_measure_device");
            entity.Property(e => e.DeviceType)
                .HasMaxLength(100)
                .HasColumnName("device_type");
            entity.Property(e => e.IdApartment).HasColumnName("id_apartment");

            entity.HasOne(d => d.IdApartmentNavigation).WithMany(p => p.Measuredevices)
                .HasForeignKey(d => d.IdApartment)
                .HasConstraintName("FK_Device_Apartment");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.IdProperty).HasName("PK__Property__F65F01842E446938");

            entity.ToTable("Property");

            entity.Property(e => e.IdProperty).HasColumnName("id_property");
            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PostOfficeName)
                .HasMaxLength(50)
                .HasColumnName("post_office_name");
            entity.Property(e => e.Postcode)
                .HasMaxLength(5)
                .HasColumnName("postcode");
            entity.Property(e => e.Streetname)
                .HasMaxLength(100)
                .HasColumnName("streetname");
            entity.Property(e => e.Streetnumber)
                .HasMaxLength(10)
                .HasColumnName("streetnumber");

            entity.HasOne(d => d.IdUserNavigation).WithMany(p => p.Properties)
                .HasForeignKey(d => d.IdUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Property_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.IdUser).HasName("PK__Users__D2D14637B0862C70");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164C4557A2D").IsUnique();

            entity.Property(e => e.IdUser).HasColumnName("id_user");
            entity.Property(e => e.Apartment)
                .HasMaxLength(10)
                .HasColumnName("apartment");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Firstname)
                .HasMaxLength(50)
                .HasColumnName("firstname");
            entity.Property(e => e.Lastname)
                .HasMaxLength(50)
                .HasColumnName("lastname");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.PostOfficeName)
                .HasMaxLength(50)
                .HasColumnName("post_office_name");
            entity.Property(e => e.Postcode)
                .HasMaxLength(5)
                .HasColumnName("postcode");
            entity.Property(e => e.Streetname)
                .HasMaxLength(100)
                .HasColumnName("streetname");
            entity.Property(e => e.Streetnumber)
                .HasMaxLength(10)
                .HasColumnName("streetnumber");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .HasColumnName("password");

        });

        modelBuilder.Entity<UserAccess>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserAcce__3213E83F80F014C6");

            entity.ToTable("UserAccess");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.FromDate).HasColumnName("from_date");
            entity.Property(e => e.IdApartment).HasColumnName("id_apartment");
            entity.Property(e => e.IdUser).HasColumnName("id_user");

            entity.HasOne(d => d.IdApartmentNavigation).WithMany(p => p.UserAccesses)
                .HasForeignKey(d => d.IdApartment)
                .HasConstraintName("FK_Access_Apartment");

            entity.HasOne(d => d.IdUserNavigation).WithMany(p => p.UserAccesses)
                .HasForeignKey(d => d.IdUser)
                .HasConstraintName("FK_Access_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
