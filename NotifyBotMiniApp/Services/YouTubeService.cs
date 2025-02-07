using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NotifyBotMiniApp.Models;
using System.Text.Json;

namespace NotifyBotMiniApp.Services
{
	public class YouTubeService
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;

		public YouTubeService(HttpClient httpClient, IConfiguration configuration)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		public async Task<List<YouTubeChannel>> SearchChannels(string channelName)
		{
			// URL для поиска каналов
			string searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&type=channel&q={Uri.EscapeDataString(channelName)}&key={_configuration["YouTube:ApiKey"]}";

			_httpClient.Timeout = TimeSpan.FromMinutes(5);
			var searchResponse = await _httpClient.GetStringAsync(searchUrl);
			dynamic searchJson = JsonConvert.DeserializeObject(searchResponse);

			var channels = new List<YouTubeChannel>();

			foreach (var item in searchJson.items)
			{
				string channelId = (string)item.id.channelId;

				// URL для получения статистики канала
				string statsUrl = $"https://www.googleapis.com/youtube/v3/channels?part=statistics&id={channelId}&key={_configuration["YouTube:ApiKey"]}";
				var statsResponse = await _httpClient.GetStringAsync(statsUrl);
				dynamic statsJson = JsonConvert.DeserializeObject(statsResponse);

				var subscriberCount = statsJson.items[0].statistics.subscriberCount;

				channels.Add(new YouTubeChannel
				{
					Id = channelId,
					Title = (string)item.snippet.title,
					ThumbnailUrl = (string)item.snippet.thumbnails.medium.url,
					SubscriberCount = subscriberCount != null ? Convert.ToInt64(subscriberCount) : 0
				});
			}

			return channels;
		}

		public async Task AddSubscription(long? chatId, string? username, string channelId, string? channelName, ApplicationDbContext dbContext)
		{
			if (chatId == null)
			{
				throw new Exception("Время сессии истекло. Вернитесь в главное меню или перезапустите приложение");
			}

			// Поиск пользователя по ChatId
			var user = await dbContext.Users
				.FirstOrDefaultAsync(u => u.ChatId == chatId);

			// Если пользователь не найден, создаём его
			if (user == null)
			{
				user = new User
				{
					ChatId = Convert.ToInt64(chatId),
					Username = username
				};

				dbContext.Users.Add(user);
				await dbContext.SaveChangesAsync();
			}

			// Проверяем, есть ли уже подписка на этот YouTube канал для данного пользователя
			bool subscriptionExists = await dbContext.YouTube_Subscriptions
				.AnyAsync(s => s.UserId == user.UserId && s.ChannelId == channelId);

			if (!subscriptionExists)
			{
				var subscription = new YouTube_Subscription
				{
					UserId = user.UserId,
					ChannelId = channelId,
					ChannelName = channelName,
					SubscribedAt = DateTime.UtcNow
				};

				dbContext.YouTube_Subscriptions.Add(subscription);
				await dbContext.SaveChangesAsync();
			}
			else
			{
				throw new Exception($"Вы уже подписаны на канал: {channelName}");
			}
		}

		public async Task Unsubscribe(long? chatId, string channelId, ApplicationDbContext dbContext)
		{
			if (chatId == null)
			{
				throw new Exception("Время сессии истекло. Вернитесь в главное меню или перезапустите приложение");
			}

			// Поиск пользователя
			var user = await dbContext.Users.FirstOrDefaultAsync(u => u.ChatId == chatId);
			if (user == null)
			{
				throw new Exception("У вас нет подписок.");
			}

			// Поиск подписки
			var subscription = await dbContext.YouTube_Subscriptions
				.FirstOrDefaultAsync(sub => sub.UserId == user.UserId && sub.ChannelId == channelId);

			if (subscription == null)
			{
				throw new Exception("Подписка не найдена.");
			}

			// Удаление подписки
			dbContext.YouTube_Subscriptions.Remove(subscription);
			await dbContext.SaveChangesAsync();
		}

		public async Task<List<YouTube_Subscription>> GetMySubscriptions(long? chatId, ApplicationDbContext dbContext)
		{
			var subscriptions = await dbContext.YouTube_Subscriptions
				.Where(sub => sub.User.ChatId == chatId)
				.OrderBy(sub => sub.ChannelName)
				.ToListAsync();

			return subscriptions;
		}

		public async Task<List<YouTubeVideo>> GetLatestUpdates(string channelId, DateTime lastChecked)
		{
			var requestUri = $"https://www.googleapis.com/youtube/v3/search?part=snippet&channelId={channelId}&order=date&type=video&publishedAfter={lastChecked:yyyy-MM-ddTHH:mm:ssZ}&key={_configuration["YouTube:ApiKey"]}";
			var response = await _httpClient.GetAsync(requestUri);

			if (!response.IsSuccessStatusCode) return new List<YouTubeVideo>();

			var content = await response.Content.ReadAsStringAsync();
			var jsonDoc = JsonDocument.Parse(content);
			var root = jsonDoc.RootElement;

			var videos = new List<YouTubeVideo>();

			if (root.TryGetProperty("items", out var items))
			{
				foreach (var item in items.EnumerateArray())
				{
					var videoId = item.GetProperty("id").GetProperty("videoId").GetString();
					var snippet = item.GetProperty("snippet");
					var title = snippet.GetProperty("title").GetString();
					var streamStatus = snippet.GetProperty("liveBroadcastContent").GetString();
					var publishedAt = snippet.GetProperty("publishedAt").GetDateTime();

					videos.Add(new YouTubeVideo
					{
						VideoId = videoId,
						Title = title,
						StreamStatus = streamStatus,
						PublishedAt = publishedAt
					});
				}
			}

			return videos;
		}
	}

	public class YouTubeChannel
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public string ThumbnailUrl { get; set; }
		public long SubscriberCount { get; set; }
	}

	public class YouTubeVideo
	{
		public string VideoId { get; set; }
		public string Title { get; set; }
		public string StreamStatus { get; set; }
		public DateTime? PublishedAt { get; set; }
	}
}
