using ASC.DataAccess.Interfaces;
using ASC.DataAccess;
using ASC.Solution.Configuration;
using ASC.Solution.Data;
using ASC.Solution.Services;
using ASC.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ASC.Business.Interfaces;
using ASC.Business;


namespace ASC.Web.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddConfig(this IServiceCollection services, IConfiguration config)
        {

            // Add AddDbContext with connectionString to mirage database

            var connectionString = config.GetConnectionString("DefaultConnection") ??

                                   throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

            //Add Options and get data from appsettings.json with "AppSettings"

            services.AddOptions(); // 10ption

            services.Configure<ApplicationSettings>(config.GetSection("AppSettings"));

            // Using a Gmail Authentication Provider for Customer Authentication
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    IConfigurationSection googleAuthNSection = config.GetSection("Authentication:Google");
                    options.ClientId = config["Google:Identity:ClientId"];
                    options.ClientSecret = config["Google:Identity:ClientSecret"];
                });
            //services.AddDistributedMemoryCache();
            services.AddStackExchangeRedisCache(options =>
            {
                var redisConfig = config.GetSection("CacheSettings");
                options.Configuration = redisConfig["CacheConnectionString"];
                options.InstanceName = redisConfig["CacheInstance"];
            });

            return services;

        }

        //Add service

        public static IServiceCollection AddMyDependencyGroup(this IServiceCollection services)
        {

            //Add ApplicationDbContext

            services.AddScoped<DbContext, ApplicationDbContext>();

            //Add Identity User IdentityUser

            services.AddIdentity<IdentityUser, IdentityRole>((options) =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            //Add services

            services.AddTransient<IEmailSender, AuthMessageSender>();

            services.AddTransient<ISmsSender, AuthMessageSender>();

            services.AddSingleton<IIdentitySeed, IdentitySeed>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddSession();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDistributedMemoryCache();
            services.AddSingleton<INavigationCacheOperations, NavigationCacheOperations>();
            services.AddScoped<IMasterDataCacheOperations,MasterDataCacheOperations>();
            services.AddScoped<IServiceRequestOperations, ServiceRequestOperations>();

            //Add RazorPages, MVC

            // services.AddRazorPages();
            services.AddRazorPages()
     .AddMvcOptions(options => { })
     .AddCookieTempDataProvider(); // ✅ Bắt buộc
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddControllersWithViews().AddJsonOptions(options =>{
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.DictionaryKeyPolicy = null;
            });
            //Add MasterDataOperations
            services.AddScoped<IMasterDataOperations, MasterDataOperations>();
            services.AddAutoMapper(typeof(ApplicationDbContext));
            return services;
        }
    }
}