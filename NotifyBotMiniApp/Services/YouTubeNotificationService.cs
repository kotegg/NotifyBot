using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using System.Text.Json;

namespace NotifyBotMiniApp.Services
{
    public class YouTubeNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITelegramBotClient _botClient;
        private readonly YouTubeService _youTubeService;
        private readonly ILogger<YouTubeNotificationService> _logger;

        public YouTubeNotificationService(IServiceProvider serviceProvider, ITelegramBotClient botClient, YouTubeService youTubeService, ILogger<YouTubeNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _botClient = botClient;
            _youTubeService = youTubeService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("YouTubeNotificationService запущен.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Начало цикла проверки YouTube каналов...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Получаем список уникальных каналов
                        var youtubeChannels = dbContext.YouTube_Subscriptions
                            .Select(s => new { s.ChannelId, s.ChannelName })
                            .Distinct()
                            .ToList();

                        foreach (var channel in youtubeChannels)
                        {
                            try
                            {
                                // Проверяем новые видео начиная с текущего времени минус интервал опроса
                                DateTime checkFrom = DateTime.UtcNow.AddMinutes(-5);

                                var newVideos = await _youTubeService.GetLatestUpdates(channel.ChannelId, checkFrom);

                                if (newVideos.Any())
                                {
                                    // Собираем список подписчиков канала
                                    var subscribers = dbContext.YouTube_Subscriptions
                                        .Where(s => s.ChannelId == channel.ChannelId)
                                        .Select(s => s.UserId)
                                        .ToList();

                                    foreach (var video in newVideos)
                                    {
                                        foreach (var userId in subscribers)
                                        {
                                            // Получаем ChatId из таблицы Users
                                            var chatId = dbContext.Users
                                                .Where(u => u.UserId == userId)
                                                .Select(u => u.ChatId)
                                                .FirstOrDefault();

                                            string notificationText;
                                            string linkText;

                                            if (chatId != null)
                                            {
                                                if (video.StreamStatus == "none")
                                                {
                                                    notificationText = "📹 Новое видео на канале";
                                                    linkText = "Ссылка на видео";
                                                }
                                                else
                                                {
                                                    notificationText = "📹 Началась трансляция на канале";
                                                    linkText = "Ссылка на стрим";
                                                }

                                                await _botClient.SendTextMessageAsync(
                                                    chatId,
                                                    $"{notificationText} {channel.ChannelName}:\n<b>{video.Title}</b>\n🔗 <a href=\"https://youtu.be/{video.VideoId}\">{linkText}</a>",
                                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                                                );
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Ошибка при обработке YouTube канала {channel.ChannelId}");
                            }
                        }
                    }

                    _logger.LogInformation("Ожидание перед следующей проверкой YouTube...");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Фатальная ошибка в YouTubeNotificationService.");
            }
        }
    }
}
