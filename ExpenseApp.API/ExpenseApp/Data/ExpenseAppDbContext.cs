using Microsoft.EntityFrameworkCore;
using ExpenseApp.Models;

namespace ExpenseApp.Data
{
    public class ExpenseAppDbContext : DbContext
    {
        public ExpenseAppDbContext(DbContextOptions<ExpenseAppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ExpenseForm> ExpenseForms { get; set; }
        public DbSet<ExpenseItem> ExpenseItems { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Self-referencing relationship: User -> Manager
            modelBuilder.Entity<User>()
                .HasOne(u => u.Manager)
                .WithMany(u => u.Employees)
                .HasForeignKey(u => u.ManagerId)
                .OnDelete(DeleteBehavior.Restrict); // taake cascade delete loop na bane

            // ExpenseForm -> Employee
            modelBuilder.Entity<ExpenseForm>()
                .HasOne(ef => ef.Employee)
                .WithMany(u => u.ExpenseForms)
                .HasForeignKey(ef => ef.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExpenseItem -> ExpenseForm
            modelBuilder.Entity<ExpenseItem>()
                .HasOne(ei => ei.ExpenseForm)
                .WithMany(ef => ef.ExpenseItems)
                .HasForeignKey(ei => ei.ExpenseFormId)
                .OnDelete(DeleteBehavior.Cascade); // form delete ho to items bhi delete ho

            // ApprovalHistory -> ExpenseForm
            modelBuilder.Entity<ApprovalHistory>()
                .HasOne(ah => ah.ExpenseForm)
                .WithMany()
                .HasForeignKey(ah => ah.ExpenseFormId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApprovalHistory -> ActionByUser
            modelBuilder.Entity<ApprovalHistory>()
                .HasOne(ah => ah.ActionByUser)
                .WithMany()
                .HasForeignKey(ah => ah.ActionByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ensure username is unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Fix decimal precision warning
            modelBuilder.Entity<ExpenseItem>()
                .Property(e => e.Amount)
                .HasPrecision(18, 2);
        }
    }
}