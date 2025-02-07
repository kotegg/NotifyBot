using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Moq;
using NotifyBotMiniApp.Services;
using NotifyBotMiniApp.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NotifyBotMiniApp.Tests
{
	public class YouTubeServiceTests
	{
		private static Microsoft.Extensions.Configuration.IConfiguration _configuration;
		private static HttpClient _httpClient;

		static YouTubeServiceTests()
		{
			// Мокируем IConfiguration
			var mockConfiguration = new Mock<IConfiguration>();
			mockConfiguration.SetupGet(config => config["YouTube:ApiKey"]).Returns("FAKE_API_KEY");
			_configuration = mockConfiguration.Object;

			// Используем MockHttpMessageHandler
			var mockHandler = new MockHttpMessageHandler(request =>
			{
				return new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent("{ \"items\": [] }")
				};
			});

			_httpClient = new HttpClient(mockHandler);
		}

		[Fact]
		public async Task SearchChannels_ShouldReturnEmptyList_WhenNoChannelsFound()
		{
			// Arrange
			var youTubeService = new YouTubeService(_httpClient, _configuration);

			// Act
			var result = await youTubeService.SearchChannels("NonExistentChannel");

			// Assert
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public async Task AddSubscription_AddsNewSubscription_WhenUserDoesNotExist()
		{
			// Arrange
			var dbContext = TestHelpers.CreateDbContext();
			var youTubeService = new YouTubeService(_httpClient, _configuration);

			var chatId = 1L;
			var username = "TestUser";
			var channelId = "123";
			var channelName = "Test Channel";

			// Act
			await youTubeService.AddSubscription(chatId, username, channelId, channelName, dbContext);

			// Assert
			// Проверяем, что пользователь добавлен
			var user = await dbContext.Users.FirstOrDefaultAsync(u => u.ChatId == chatId);

			Assert.NotNull(user);
			Assert.Equal(chatId, user.ChatId);
			Assert.Equal(username, user.Username);

			// Проверяем, что подписка добавлена
			var subscription = await dbContext.YouTube_Subscriptions
				.FirstOrDefaultAsync(s => s.UserId == user.UserId && s.ChannelId == channelId);

			Assert.NotNull(subscription);
			Assert.Equal(channelId, subscription.ChannelId);
			Assert.Equal(channelName, subscription.ChannelName);
		}
	}
}
