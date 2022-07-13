using Microsoft.EntityFrameworkCore;
using OnlineBuildingGame.Models;

namespace OnlineBuildingGame.Data
{
    public class GameDbContext: DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options): base(options){}

        public DbSet<EntityModel> Entities { get; set; }
        public DbSet<InventoryDataModel> InventoryData { get; set; }
        public DbSet<InventoryModel> Inventories { get; set; }
        public DbSet<MapDataModel> MapData { get; set; }
        public DbSet<MapModel> Maps { get; set; }
        public DbSet<ProtectedTileModel> ProtectedTiles { get; set; }
        public DbSet<PlayerModel> Players { get; set; }  
        public DbSet<PlayerLocationModel> PlayerLocations { get; set; }
    }
}
