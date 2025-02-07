using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NotifyBotMiniApp.Models;

public class TwitchService
{
	private readonly HttpClient _httpClient;
	private readonly IConfiguration _configuration;
		
	public TwitchService(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}

	// TwitchChannels
	public async Task<bool> IsSubscribedChannel(long? chatId, string twitchChannel, ApplicationDbContext dbContext)
	{
		var isSubscribed = await dbContext.Twitch_Subscriptions
			.AnyAsync(subscription =>
				subscription.User.ChatId == chatId &&
				subscription.TwitchChannel == twitchChannel);

		return isSubscribed;
	}

	public async Task<List<Twitch_Subscription>> GetMyChannelSubscriptions(long? chatId, ApplicationDbContext dbContext)
	{
		var subscriptions = await dbContext.Twitch_Subscriptions
			.Where(sub => sub.User.ChatId == chatId)
			.OrderBy(sub => sub.TwitchChannel)
			.ToListAsync();

		return subscriptions;
	}

	public async Task SubscribeToTwitchChannel(long? chatId, string twitchChannel, string? username, ApplicationDbContext dbContext)
	{
		if (chatId == null)
		{
			throw new Exception("Время сессии истекло. Вернитесь в главное меню или перезапустите приложение");
		}
		// Поиск пользователя по ChatId
		var user = await dbContext.Users
			.FirstOrDefaultAsync(u => u.ChatId == chatId);

		// Если пользователя нет, создаём его
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

		// Проверяем, есть ли уже подписка на этот Twitch канал для данного пользователя
		bool subscriptionExists = await dbContext.Twitch_Subscriptions
			.AnyAsync(s => s.UserId == user.UserId && s.TwitchChannel == twitchChannel);

		if (!subscriptionExists)
		{
			// Если подписки нет, добавляем её
			var subscription = new Twitch_Subscription
			{
				UserId = user.UserId,
				TwitchChannel = twitchChannel,
				SubscribedAt = DateTime.UtcNow
			};

			dbContext.Twitch_Subscriptions.Add(subscription);
			await dbContext.SaveChangesAsync();
		}
	}

	public async Task UnsubscribeToTwitchChannel(long? chatId, string twitchChannel, ApplicationDbContext dbContext)
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
		var subscription = await dbContext.Twitch_Subscriptions
			.FirstOrDefaultAsync(sub => sub.UserId == user.UserId && sub.TwitchChannel == twitchChannel);

		if (subscription == null)
		{
			throw new Exception("Подписка не найдена.");
		}

		// Удаление подписки
		dbContext.Twitch_Subscriptions.Remove(subscription);
		await dbContext.SaveChangesAsync();
	}

	public async Task<string> GetAccessToken()
	{
		var requestUri = $"{_configuration["Twitch:TokenUrl"]}?client_id={_configuration["Twitch:ClientId"]}&client_secret={_configuration["Twitch:ClientSecret"]}&grant_type=client_credentials";

		var response = await _httpClient.PostAsync(requestUri, null);
		var content = await response.Content.ReadAsStringAsync();

		var json = JObject.Parse(content);
		return json["access_token"].ToString();
	}

	public async Task<(string streamInfo, DateTime? startedAt)> CheckStreamStatus(string twitchChannel, string accessToken)
	{
		_httpClient.DefaultRequestHeaders.Clear();
		_httpClient.DefaultRequestHeaders.Add("Client-Id", _configuration["Twitch:ClientId"]);
		_httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

		var response = await _httpClient.GetAsync($"{_configuration["Twitch:StreamsUrl"]}?user_login={twitchChannel}");
		var content = await response.Content.ReadAsStringAsync();

		var json = JObject.Parse(content);
		var data = json["data"];

		if (data != null)
		{
			if (data.HasValues)
			{
				var stream = data[0];
				string title = stream["title"].ToString();
				string gameName = stream["game_name"].ToString();
				twitchChannel = stream["user_name"].ToString();
				DateTime startedAt = DateTime.Parse(stream["started_at"].ToString());

				string streamInfo = $"Пользователь {twitchChannel} сейчас стримит:\n {title} \nИграет в: {gameName}\n";
				return (streamInfo, startedAt);
			}
			else
			{
				return ($"Пользователь {twitchChannel} не стримит в данный момент.", null);
			}
		}
		else
		{
			return ($"Пользователь {twitchChannel} не найден!", null);
		}
	}
}
