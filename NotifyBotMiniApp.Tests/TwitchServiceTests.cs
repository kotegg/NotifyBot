using Castle.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Moq;
using NotifyBotMiniApp.Models;
using NotifyBotMiniApp.Services;
using NotifyBotMiniApp.Tests;
using System.Net;
using System.Net.Http;

namespace NotifyBotMiniApp.Tests
{
	public class TwitchServiceTests
	{
		private static Microsoft.Extensions.Configuration.IConfiguration _configuration;
		private static HttpClient _httpClient;

		static TwitchServiceTests()
		{
			// Инициализация конфигурации и HttpClient один раз для всех тестов
			_configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
			{
				{ "Twitch:ClientId", "test_client_id" },
				{ "Twitch:TokenUrl", "http://test-token-url.com/token" },
				{ "Twitch:StreamsUrl", "http://test-url.com/streams" }
			}).Build();
			_httpClient = new HttpClient(); // Или используйте mock HttpClient
		}

		[Fact]
		public async Task IsSubscribedChannel_ReturnsTrue_WhenSubscribed()
		{
			// Arrange
			var dbContext = TestHelpers.CreateDbContext();
			var chatId = 12345L;
			var twitchChannel = "TestChannel";

			var user = new User
			{
				ChatId = chatId,
				UserId = 1,
				Username = "TestUser"
			};

			var subscription = new Twitch_Subscription
			{
				UserId = user.UserId,
				TwitchChannel = twitchChannel
			};

			dbContext.Users.Add(user);
			dbContext.Twitch_Subscriptions.Add(subscription);
			await dbContext.SaveChangesAsync();

			var twitchService = new TwitchService(_httpClient, _configuration);

			// Act
			var result = await twitchService.IsSubscribedChannel(chatId, twitchChannel, dbContext);

			// Assert
			Assert.True(result);
		}

		[Fact]
		public async Task CheckStreamStatus_ReturnsStreamInfo_WhenChannelIsLive()
		{
			// Arrange
			var mockHttpMessageHandler = new MockHttpMessageHandler(request =>
			{
				return new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent("{\"data\": [{\"title\": \"Test Stream\", \"game_name\": \"Test Game\", \"user_name\": \"TestUser\", \"started_at\": \"2024-12-01T10:00:00Z\"}]}")
				};
			});

			var httpClient = new HttpClient(mockHttpMessageHandler);
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string>
				{
					{ "Twitch:ClientId", "test_client_id" },
					{ "Twitch:TokenUrl", "http://test-token-url.com/token" },
					{ "Twitch:StreamsUrl", "http://test-url.com/streams" }
				})
				.Build();

			var twitchService = new TwitchService(httpClient, configuration);

			// Act
			var result = await twitchService.CheckStreamStatus("TestUser", "test_access_token");

			// Assert
			Assert.Equal("Пользователь TestUser сейчас стримит:\n Test Stream \nИграет в: Test Game\n", result.streamInfo);
			Assert.NotNull(result.startedAt);
		}
	}

	public class MockHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _handlerFunc;

		public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
		{
			_handlerFunc = handlerFunc ?? throw new ArgumentNullException(nameof(handlerFunc));
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return Task.FromResult(_handlerFunc(request));
		}
	}
}