using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tasks.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Task> Tasks { get; set; }

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
        }
    }

    public class Task
    {
        [Key] public int TaskId { get; set; }

        [StringLength(450)] public string PublicId { get; set; }

        [Required]
        [RegularExpression(@"^[^\[\]]+$", ErrorMessage = "Description must not contain [ or ]")]
        public string Description { get; set; }

        public string JiraId { get; set; }

        public bool Completed { get; set; }

        public int? AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        [InverseProperty("Tasks")]
        public virtual Account Account { get; set; }
    }

    public class Account
    {
        public Account()
        {
            Tasks = new HashSet<Task>();
        }

        [Key] public int AccountId { get; set; }

        public string PublicId { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string Role { get; set; }

        [InverseProperty(nameof(Task.Account))]
        public virtual ICollection<Task> Tasks { get; set; }
    }
}