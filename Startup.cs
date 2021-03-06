using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using OnlineBuildingGame.Data;
using OnlineBuildingGame.Game;
using OnlineBuildingGame.Hubs;

namespace OnlineBuildingGame
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<GameDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("StorageConnection")));
            services.AddDbContext<GameUsersDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("UsersConnection")));

            services.AddSingleton<LoginManager>();
            services.AddSingleton<GameWorld>();
            services.AddSingleton<GameLogic>();
            services.AddSingleton<GamePipe>();

            services.AddDefaultIdentity<IdentityUser>(
                options => {
                    options.Password.RequiredLength = 3;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                }).AddEntityFrameworkStores<GameUsersDbContext>();

            services.AddControllersWithViews();
            services.AddSignalR(o =>
            {
                o.EnableDetailedErrors = true;
                o.MaximumReceiveMessageSize = 102400000;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Pages}/{action=Index}/{id?}");

                endpoints.MapHub<GameHub>("/gameHub", configureOptions =>
                {

                });
            });
        }
    }
}
