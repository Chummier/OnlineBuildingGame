using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineBuildingGame.Models;

namespace OnlineBuildingGame.Data
{
    public class GameDbContext: DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options): base(options)
        {

        }
        public DbSet<WorldLayerModel> World { get; set; }
        public DbSet<EntityLayerModel> Entities { get; set; }
        public DbSet<PlayerModel> Players { get; set; }
        public DbSet<InventoryModel> Inventories { get; set; }
        public DbSet<HotbarModel> Hotbars { get; set; }
    }
}
