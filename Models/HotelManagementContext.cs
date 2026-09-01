using System;
using System.Collections.Generic;
using HotelManagementSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Models;

public partial class HotelManagementContext : DbContext
{
    public HotelManagementContext(DbContextOptions<HotelManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<OperationLog> OperationLogs { get; set; }

    public virtual DbSet<OperationType> OperationTypes { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    public virtual DbSet<StayRecord> StayRecords { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingNumber);

            entity.HasIndex(e => new { e.BranchId, e.RoomTypeId, e.BookingStatus, e.CheckInDate, e.CheckOutDate }, "IX_Bookings_Availability");

            entity.HasIndex(e => new { e.BranchId, e.CreatedAt }, "IX_Bookings_Branch_CreatedAt");

            entity.HasIndex(e => new { e.BookingStatus, e.CheckOutDate }, "IX_Bookings_NoShow");

            entity.Property(e => e.BookingNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.BookerName).HasMaxLength(50);
            entity.Property(e => e.BookingStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CancellationCause)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.CancelledAt).HasPrecision(0);
            entity.Property(e => e.CancelledByEmployeeNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(CONVERT([datetime2](0),(sysdatetimeoffset() AT TIME ZONE 'Taipei Standard Time')))", "DF_Bookings_CreatedAt");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .IsUnicode(false);
            entity.Property(e => e.NightlyPriceSnapshot).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RoomTypeNameSnapshot).HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.CancelledByEmployeeNumberNavigation).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CancelledByEmployeeNumber)
                .HasConstraintName("FK_Bookings_CancelledByEmployee");

            entity.HasOne(d => d.RoomType).WithMany(p => p.Bookings)
                .HasPrincipalKey(p => new { p.RoomTypeId, p.BranchId })
                .HasForeignKey(d => new { d.RoomTypeId, d.BranchId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_RoomTypes");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasIndex(e => e.BranchName, "UQ_Branches_BranchName").IsUnique();

            entity.Property(e => e.AcceptsNewBookings).HasDefaultValue(true, "DF_Branches_AcceptsNewBookings");
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.BranchName).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(2048);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Region).HasMaxLength(50);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeNumber);

            entity.HasIndex(e => e.BranchId, "IX_Employees_BranchId");

            entity.Property(e => e.EmployeeNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Employees_IsActive");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Branch).WithMany(p => p.Employees)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_Employees_Branches");
        });

        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.HasIndex(e => new { e.OperatorEmployeeNumber, e.OperatedAt }, "IX_OperationLogs_Operator_OperatedAt").IsDescending(false, true);

            entity.HasIndex(e => new { e.TargetBranchId, e.OperatedAt }, "IX_OperationLogs_TargetBranch_OperatedAt").IsDescending(false, true);

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.OperatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(CONVERT([datetime2](0),(sysdatetimeoffset() AT TIME ZONE 'Taipei Standard Time')))", "DF_OperationLogs_OperatedAt");
            entity.Property(e => e.OperatorEmployeeNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TargetIdentifier).HasMaxLength(100);
            entity.Property(e => e.TargetType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.OperationType).WithMany(p => p.OperationLogs)
                .HasForeignKey(d => d.OperationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OperationLogs_OperationTypes");

            entity.HasOne(d => d.OperatorEmployeeNumberNavigation).WithMany(p => p.OperationLogs)
                .HasForeignKey(d => d.OperatorEmployeeNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OperationLogs_OperatorEmployee");

            entity.HasOne(d => d.TargetBranch).WithMany(p => p.OperationLogs)
                .HasForeignKey(d => d.TargetBranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OperationLogs_TargetBranch");
        });

        modelBuilder.Entity<OperationType>(entity =>
        {
            entity.HasIndex(e => e.OperationTypeCode, "UQ_OperationTypes_OperationTypeCode").IsUnique();

            entity.HasIndex(e => e.OperationTypeName, "UQ_OperationTypes_OperationTypeName").IsUnique();

            entity.Property(e => e.OperationTypeCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OperationTypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasIndex(e => new { e.BranchId, e.RoomTypeId, e.SupplyStatus, e.CleaningStatus }, "IX_Rooms_CheckInCandidate");

            entity.HasIndex(e => new { e.BranchId, e.RoomNumber }, "UQ_Rooms_BranchId_RoomNumber").IsUnique();

            entity.Property(e => e.CleaningStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Clean", "DF_Rooms_CleaningStatus");
            entity.Property(e => e.DisabledReason).HasMaxLength(200);
            entity.Property(e => e.RoomNumber).HasMaxLength(10);
            entity.Property(e => e.SupplyStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Open", "DF_Rooms_SupplyStatus");

            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasPrincipalKey(p => new { p.RoomTypeId, p.BranchId })
                .HasForeignKey(d => new { d.RoomTypeId, d.BranchId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_RoomTypes");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.HasIndex(e => new { e.BranchId, e.RoomTypeName }, "UQ_RoomTypes_BranchId_RoomTypeName").IsUnique();

            entity.HasIndex(e => new { e.RoomTypeId, e.BranchId }, "UQ_RoomTypes_RoomTypeId_BranchId").IsUnique();

            entity.Property(e => e.BedType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(2048);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_RoomTypes_IsActive");
            entity.Property(e => e.NightlyPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RoomTypeName).HasMaxLength(50);

            entity.HasOne(d => d.Branch).WithMany(p => p.RoomTypes)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoomTypes_Branches");
        });

        modelBuilder.Entity<StayRecord>(entity =>
        {
            entity.HasIndex(e => e.BookingNumber, "UQ_StayRecords_BookingNumber").IsUnique();

            entity.HasIndex(e => new { e.RoomId, e.ActualCheckOutAt }, "UX_StayRecords_ActiveRoom")
                .IsUnique()
                .HasFilter("([ActualCheckOutAt] IS NULL)");

            entity.Property(e => e.ActualCheckInAt).HasPrecision(0);
            entity.Property(e => e.ActualCheckOutAt).HasPrecision(0);
            entity.Property(e => e.BookingNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CheckedInByEmployeeNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CheckedOutByEmployeeNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PrimaryGuestName).HasMaxLength(50);
            entity.Property(e => e.RoomNumberSnapshot).HasMaxLength(10);

            entity.HasOne(d => d.BookingNumberNavigation).WithOne(p => p.StayRecord)
                .HasForeignKey<StayRecord>(d => d.BookingNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StayRecords_Bookings");

            entity.HasOne(d => d.CheckedInByEmployeeNumberNavigation).WithMany(p => p.StayRecordCheckedInByEmployeeNumberNavigations)
                .HasForeignKey(d => d.CheckedInByEmployeeNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StayRecords_CheckedInByEmployee");

            entity.HasOne(d => d.CheckedOutByEmployeeNumberNavigation).WithMany(p => p.StayRecordCheckedOutByEmployeeNumberNavigations)
                .HasForeignKey(d => d.CheckedOutByEmployeeNumber)
                .HasConstraintName("FK_StayRecords_CheckedOutByEmployee");

            entity.HasOne(d => d.Room).WithMany(p => p.StayRecords)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StayRecords_Rooms");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
