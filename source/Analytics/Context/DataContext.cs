using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<Task> Tasks { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Cyrillic_General_CI_AS");

            modelBuilder.Entity<Transaction>(entity =>
            {
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

        public string AccountId { get; set; }

        public float AssignCost { get; set; }

        public float CompleteCost { get; set; }

        [InverseProperty(nameof(Transaction.Task))]
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

        public string AccountId { get; set; }

        public int TaskId { get; set; }

        public DateTimeOffset Date { get; set; }

        [ForeignKey(nameof(TaskId))]
        [InverseProperty(nameof(Context.Task.Transactions))]
        public virtual Task Task { get; set; }
    }
}