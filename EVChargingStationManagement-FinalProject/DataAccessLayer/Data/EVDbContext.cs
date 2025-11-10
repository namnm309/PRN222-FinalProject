using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Data
{
	public class EVDbContext : DbContext
	{
		public EVDbContext(DbContextOptions<EVDbContext> op) : base(op) { }

		//Khai báo entities tại đây dùm
		public DbSet<Users> Users { get; set; }
		public DbSet<RefreshToken> RefreshTokens { get; set; }
		public DbSet<ChargingStation> ChargingStations { get; set; }
		public DbSet<ChargingSpot> ChargingSpots { get; set; }
		public DbSet<StationMaintenance> StationMaintenances { get; set; }
		public DbSet<StationError> StationErrors { get; set; }
		public DbSet<Customer> Customers { get; set; }
		public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
		public DbSet<UserSubscription> UserSubscriptions { get; set; }
		public DbSet<StationStaffAssignment> StationStaffAssignments { get; set; }
		public DbSet<ChargingSession> ChargingSessions { get; set; }


		//Cấu hình chi tiết entities thì tại đây
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Configure RefreshToken entity
			modelBuilder.Entity<RefreshToken>(entity =>
			{
				entity.HasIndex(e => e.Token).IsUnique();
				entity.HasIndex(e => e.UserId);
				
				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// Configure ChargingStation entity
			modelBuilder.Entity<ChargingStation>(entity =>
			{
				entity.HasIndex(e => e.Name);
				entity.HasIndex(e => e.Status);
			});

			// Configure ChargingSpot entity
			modelBuilder.Entity<ChargingSpot>(entity =>
			{
				entity.HasIndex(e => e.ChargingStationId);
				entity.HasIndex(e => e.SpotNumber);
				entity.HasIndex(e => e.Status);
				
				entity.HasOne(e => e.ChargingStation)
					.WithMany(s => s.ChargingSpots)
					.HasForeignKey(e => e.ChargingStationId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			// Configure StationMaintenance entity
			modelBuilder.Entity<StationMaintenance>(entity =>
			{
				entity.HasIndex(e => e.ChargingStationId);
				entity.HasIndex(e => e.ChargingSpotId);
				entity.HasIndex(e => e.ReportedByUserId);
				entity.HasIndex(e => e.AssignedToUserId);
				entity.HasIndex(e => e.Status);
				entity.HasIndex(e => e.ScheduledDate);
				
				entity.HasOne(e => e.ChargingStation)
					.WithMany()
					.HasForeignKey(e => e.ChargingStationId)
					.OnDelete(DeleteBehavior.Restrict);
				
				entity.HasOne(e => e.ChargingSpot)
					.WithMany()
					.HasForeignKey(e => e.ChargingSpotId)
					.OnDelete(DeleteBehavior.Restrict);
				
				entity.HasOne(e => e.ReportedByUser)
					.WithMany()
					.HasForeignKey(e => e.ReportedByUserId)
					.OnDelete(DeleteBehavior.Restrict);
				
				entity.HasOne(e => e.AssignedToUser)
					.WithMany()
					.HasForeignKey(e => e.AssignedToUserId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			// Configure StationError entity
			modelBuilder.Entity<StationError>(entity =>
			{
				entity.HasIndex(e => e.ChargingStationId);
				entity.HasIndex(e => e.ChargingSpotId);
				entity.HasIndex(e => e.ReportedByUserId);
				entity.HasIndex(e => e.ResolvedByUserId);
				entity.HasIndex(e => e.Status);
				entity.HasIndex(e => e.ErrorCode);
				entity.HasIndex(e => e.ReportedAt);
				
				entity.HasOne(e => e.ChargingStation)
					.WithMany()
					.HasForeignKey(e => e.ChargingStationId)
					.OnDelete(DeleteBehavior.Restrict);
				
				entity.HasOne(e => e.ChargingSpot)
					.WithMany()
					.HasForeignKey(e => e.ChargingSpotId)
					.OnDelete(DeleteBehavior.Restrict);
				
				entity.HasOne(e => e.ReportedByUser)
					.WithMany()
					.HasForeignKey(e => e.ReportedByUserId)
					.OnDelete(DeleteBehavior.Restrict);
				
				entity.HasOne(e => e.ResolvedByUser)
					.WithMany()
					.HasForeignKey(e => e.ResolvedByUserId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			// Config Customer
			modelBuilder.Entity<Customer>(entity =>
			{
				entity.HasIndex(e => e.Type);
			});

			// Config SubscriptionPlan
			modelBuilder.Entity<SubscriptionPlan>(entity =>
			{
				entity.HasIndex(e => e.BillingType);
				entity.HasIndex(e => e.IsActive);
			});

			// Config UserSubscription
			modelBuilder.Entity<UserSubscription>(entity =>
			{
				entity.HasIndex(e => e.UserId);
				entity.HasIndex(e => e.SubscriptionPlanId);
				entity.HasIndex(e => e.Status);
			});

			// Config StationStaffAssignment
			modelBuilder.Entity<StationStaffAssignment>(entity =>
			{
				entity.HasIndex(e => new { e.ChargingStationId, e.UserId }).IsUnique();
			});

			// Config ChargingSession
			modelBuilder.Entity<ChargingSession>(entity =>
			{
				entity.HasIndex(e => e.ChargingStationId);
				entity.HasIndex(e => e.ChargingSpotId);
				entity.HasIndex(e => e.UserId);
				entity.HasIndex(e => e.StartTime);
				entity.HasIndex(e => e.Status);
			});
		}
	}
}
