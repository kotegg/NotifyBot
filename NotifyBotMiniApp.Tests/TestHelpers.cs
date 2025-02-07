using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifyBotMiniApp.Tests
{
	public static class TestHelpers
	{
		public static ApplicationDbContext CreateDbContext()
		{
			var options = new DbContextOptionsBuilder<ApplicationDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			return new ApplicationDbContext(options);
		}

		public static IConfiguration CreateConfiguration()
		{
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string>
				{
				{ "Twitch:StreamsUrl", "https://api.twitch.tv/helix/streams" }
				})
				.Build();

			return configuration;
		}
	}
}
