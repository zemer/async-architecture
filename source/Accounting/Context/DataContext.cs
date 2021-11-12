using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Task> Tasks { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Cyrillic_General_CI_AS");

            modelBuilder.Entity<Task>(entity =>
            {
                entity.HasOne(d => d.Account)
                      .WithMany(p => p.Tasks)
                      .HasForeignKey(d => d.AccountId)
                      .HasConstraintName("FK_Task_Account");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasOne(d => d.Account)
                      .WithMany(p => p.Transactions)
                      .HasForeignKey(d => d.AccountId)
                      .HasConstraintName("FK_Transaction_Account");

                entity.HasOne(d => d.Task)
                      .WithMany(p => p.Transactions)
                      .HasForeignKey(d => d.TaskId)
                      .HasConstraintName("FK_Transaction_Task");
            });
        }
    }

    public class Task
    {
        public Task()
        {
            Transactions = new HashSet<Transaction>();
        }

        [Key] public int TaskId { get; set; }

        [StringLength(450)] public string PublicId { get; set; }

        public string Description { get; set; }

        public string JiraId { get; set; }

        public bool Completed { get; set; }

        public int? AccountId { get; set; }

        public float AssignCost { get; set; }

        public float CompleteCost { get; set; }

        [ForeignKey(nameof(AccountId))]
        [InverseProperty(nameof(Context.Account.Tasks))]
        public virtual Account Account { get; set; }

        [InverseProperty(nameof(Transaction.Task))]
        public virtual ICollection<Transaction> Transactions { get; set; }
    }

    public class Account
    {
        public Account()
        {
            Tasks = new HashSet<Task>();
            Transactions = new HashSet<Transaction>();
        }

        [Key] public int AccountId { get; set; }

        public string PublicId { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string Role { get; set; }

        public float Bill { get; set; }

        [InverseProperty(nameof(Task.Account))]
        public virtual ICollection<Task> Tasks { get; set; }

        [InverseProperty(nameof(Transaction.Account))]
        public virtual ICollection<Transaction> Transactions { get; set; }
    }

    public class Transaction
    {
        [Key] public int TransactionId { get; set; }

        [StringLength(450)] public string PublicId { get; set; }

        /// <summary>
        /// Начислено на счет попуга
        /// </summary>
        public float Accrued { get; set; }

        /// <summary>
        /// Списано со счета попуга
        /// </summary>
        public float WrittenOff { get; set; }

        public int AccountId { get; set; }

        public int TaskId { get; set; }

        public DateTimeOffset Date { get; set; }

        [ForeignKey(nameof(AccountId))]
        [InverseProperty(nameof(Context.Account.Transactions))]
        public virtual Account Account { get; set; }

        [ForeignKey(nameof(TaskId))]
        [InverseProperty(nameof(Context.Task.Transactions))]
        public virtual Task Task { get; set; }
    }
}