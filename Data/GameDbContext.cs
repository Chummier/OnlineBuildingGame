using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OnlineBuildingGame.Data
{
    public class GameDbContext: DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options): base(options)
        {

        }
    }
}
