using Microsoft.EntityFrameworkCore;
using NotifyBotMiniApp.Services;
using Telegram.Bot;

namespace NotifyBotMiniApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add services to the container.

            builder.Services.AddSingleton<ITelegramBotClient>(provider =>
            {
                var botToken = builder.Configuration["TelegramBot:Token"];
                if (string.IsNullOrEmpty(botToken))
                {
                    throw new Exception("Telegram Bot Token не настроен.");
                }
                return new TelegramBotClient(botToken);
            });

			builder.Services.AddHttpClient<TwitchService>();
			builder.Services.AddHostedService<TwitchNotificationService>();
            builder.Services.AddHttpClient<YouTubeService>();
            builder.Services.AddHostedService<YouTubeNotificationService>();
            builder.Services.AddControllersWithViews();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
				options.Cookie.IsEssential = true;
			});

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(); // Логи в консоль
            builder.Logging.AddDebug();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSession();
            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}