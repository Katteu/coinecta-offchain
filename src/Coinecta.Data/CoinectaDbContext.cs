using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Coinecta.Data.Models;
using Coinecta.Data.Models.Reducers;
using Cardano.Sync.Data;

namespace Coinecta.Data;

public class CoinectaDbContext
(
    DbContextOptions<CoinectaDbContext> options,
    IConfiguration configuration
) : CardanoDbContext(options, configuration)
{
    private readonly IConfiguration _configuration = configuration;
    public DbSet<StakePoolByAddress> StakePoolByAddresses { get; set; }
    public DbSet<StakeRequestByAddress> StakeRequestByAddresses { get; set; }
    public DbSet<StakePositionByStakeKey> StakePositionByStakeKeys { get; set; }
    public DbSet<UtxoByAddress> UtxosByAddress { get; set; }
    public DbSet<NftByAddress> NftsByAddress { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StakePoolByAddress>(entity =>
        {
            entity.HasKey(item => new { item.Address, item.Slot, item.TxHash, item.TxIndex, item.UtxoStatus });

            entity.HasIndex(item => item.Address);
            entity.HasIndex(item => item.Slot);
            entity.HasIndex(item => item.TxHash);
            entity.HasIndex(item => item.TxIndex);
            entity.HasIndex(item => item.UtxoStatus);

            entity.OwnsOne(item => item.Amount);
        });
        
        modelBuilder.Entity<StakeRequestByAddress>(entity => 
        {
            entity.HasKey(item => new { item.Address, item.Slot, item.TxHash, item.TxIndex });

            entity.HasIndex(item => item.Address);
            entity.HasIndex(item => item.Slot);
            entity.HasIndex(item => item.TxHash);
            entity.HasIndex(item => item.TxIndex);

            entity.OwnsOne(item => item.Amount);
        });

        modelBuilder.Entity<StakePositionByStakeKey>(entity => 
        {
            entity.HasKey(item => new { item.StakeKey, item.Slot, item.TxHash, item.TxIndex, item.UtxoStatus });

            entity.HasIndex(item => item.StakeKey);
            entity.HasIndex(item => item.Slot);
            entity.HasIndex(item => item.TxHash);
            entity.HasIndex(item => item.TxIndex);
            entity.HasIndex(item => item.UtxoStatus);

            entity.OwnsOne(item => item.Amount);
            entity.OwnsOne(item => item.Interest);
        });

        modelBuilder.Entity<UtxoByAddress>(entity => 
        {
            entity.HasKey(item => item.Address);

            entity.HasIndex(item => item.LastRequested);
        });

        modelBuilder.Entity<NftByAddress>(entity => 
        {
            entity.HasKey(item => new { item.Address, item.Slot });

            entity.HasIndex(item => item.Slot);
            entity.HasIndex(item => item.Address);
        });

        base.OnModelCreating(modelBuilder);
    }
}