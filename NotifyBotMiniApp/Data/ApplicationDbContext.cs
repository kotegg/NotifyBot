using Microsoft.EntityFrameworkCore;
using NotifyBotMiniApp.Models;
using Telegram.Bot.Types;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {}

    public DbSet<NotifyBotMiniApp.Models.User> Users { get; set; }
    public DbSet<Twitch_Subscription> Twitch_Subscriptions { get; set; }
    public DbSet<YouTube_Subscription> YouTube_Subscriptions { get; set; }
}
