using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NotifyBotMiniApp.Services
{
    public class TwitchNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITelegramBotClient _botClient;
        private readonly TwitchService _twitchService; 
        private readonly ILogger<TwitchNotificationService> _logger;

        public TwitchNotificationService(IServiceProvider serviceProvider, ITelegramBotClient botClient, TwitchService twitchService, ILogger<TwitchNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _botClient = botClient;
            _twitchService = twitchService;
			_logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TwitchNotificationService запущен."); // Запись в лог

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Начало цикла рассылки Twitch уведомлений...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Получаем список уникальных Twitch-имен
                        var twitcChannels = dbContext.Twitch_Subscriptions
                            .Select(s => s.TwitchChannel)
                            .Distinct()
                            .ToList();

                        foreach (var twitchChannel in twitcChannels)
                        {
                            try
                            {
                                var accessToken = await _twitchService.GetAccessToken();
                                var stream = await _twitchService.CheckStreamStatus(twitchChannel, accessToken);

                                if (stream.startedAt != null)
                                {
                                    DateTime nowUtc = DateTime.UtcNow;

                                    if ((nowUtc - stream.startedAt.Value).TotalMinutes <= 5)
                                    {
                                        var subscribers = dbContext.Twitch_Subscriptions
                                            .Where(s => s.TwitchChannel == twitchChannel)
                                            .Select(s => s.User.ChatId)
                                            .ToList();

                                        foreach (var chatId in subscribers)
                                        {
                                            await _botClient.SendTextMessageAsync(
                                            chatId,
                                                $"📣 Стрим начался:\n{stream.streamInfo}\n🔗 <a href=\"https://www.twitch.tv/{twitchChannel}\">Ссылка на стрим</a>",
                                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                                            );
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Ошибка при обработке Twitch пользователя {twitchChannel}");
                            }
                        }
                    }

                    _logger.LogInformation("Ожидание перед следующей проверкой Twitch...");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            } 
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Фатальная ошибка в NotificationService.");
            }
        }
    }
}
