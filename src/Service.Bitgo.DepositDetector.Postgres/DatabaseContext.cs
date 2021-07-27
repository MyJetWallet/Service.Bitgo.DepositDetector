using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Postgres.Models;

namespace Service.Bitgo.DepositDetector.Postgres
{
    public class DatabaseContext : DbContext
    {
        public const string Schema = "deposits";

        private const string DepositsTableName = "deposits";

        private Activity _activity;

        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<DepositEntity> Deposits { get; set; }
        public static ILoggerFactory LoggerFactory { get; set; }

        public static DatabaseContext Create(DbContextOptionsBuilder<DatabaseContext> options)
        {
            var activity = MyTelemetry.StartActivity($"Database context {Schema}")?.AddTag("db-schema", Schema);

            var ctx = new DatabaseContext(options.Options) {_activity = activity};

            return ctx;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (LoggerFactory != null) optionsBuilder.UseLoggerFactory(LoggerFactory).EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            SetDepositEntry(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void SetDepositEntry(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DepositEntity>().ToTable(DepositsTableName);
            modelBuilder.Entity<DepositEntity>().Property(e => e.Id).UseIdentityColumn();
            modelBuilder.Entity<DepositEntity>().HasKey(e => e.Id);
            modelBuilder.Entity<DepositEntity>().Property(e => e.BrokerId).HasMaxLength(128);
            modelBuilder.Entity<DepositEntity>().Property(e => e.ClientId).HasMaxLength(128);
            modelBuilder.Entity<DepositEntity>().Property(e => e.WalletId).HasMaxLength(128);
            modelBuilder.Entity<DepositEntity>().Property(e => e.TransactionId).HasMaxLength(128);
            modelBuilder.Entity<DepositEntity>().Property(e => e.Amount);
            modelBuilder.Entity<DepositEntity>().Property(e => e.AssetSymbol).HasMaxLength(64);
            modelBuilder.Entity<DepositEntity>().Property(e => e.Comment).HasMaxLength(512);
            modelBuilder.Entity<DepositEntity>().Property(e => e.Integration).HasMaxLength(64);
            modelBuilder.Entity<DepositEntity>().Property(e => e.Txid).HasMaxLength(256);
            modelBuilder.Entity<DepositEntity>().Property(e => e.Status).HasDefaultValue(DepositStatus.New);
            modelBuilder.Entity<DepositEntity>().Property(e => e.MatchingEngineId).HasMaxLength(256).IsRequired(false);
            modelBuilder.Entity<DepositEntity>().Property(e => e.LastError).HasMaxLength(1024).IsRequired(false);
            modelBuilder.Entity<DepositEntity>().Property(e => e.RetriesCount).HasDefaultValue(0);
            modelBuilder.Entity<DepositEntity>().Property(e => e.EventDate);

            modelBuilder.Entity<DepositEntity>().HasIndex(e => e.Status);
            modelBuilder.Entity<DepositEntity>().HasIndex(e => e.TransactionId).IsUnique();
        }

        public async Task<int> InsertAsync(DepositEntity entity)
        {
            var result = await Deposits.Upsert(entity).On(e => e.TransactionId).NoUpdate().RunAsync();
            return result;
        }

        public async Task UpdateAsync(IEnumerable<DepositEntity> entities)
        {
            Deposits.UpdateRange(entities);
            await SaveChangesAsync();
        }
    }
}