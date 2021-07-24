using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace OnlineBuildingGame.Data
{
    public class GameUsersDbContext: IdentityDbContext
    {
        public GameUsersDbContext(DbContextOptions<GameUsersDbContext> options): base(options)
        {

        }
    }
}
