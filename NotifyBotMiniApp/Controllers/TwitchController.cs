using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotifyBotMiniApp.Services;

namespace NotifyBotMiniApp.Controllers
{
    public class TwitchController : BaseController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly TwitchService _twitchService;

        public TwitchController(ApplicationDbContext dbContext, TwitchService twitchService)
        {
            _dbContext = dbContext;
            _twitchService = twitchService;
        }

        public IActionResult Twitch()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Subscribe()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string twitchChannel)
        {
            try
            {
                var accessToken = await _twitchService.GetAccessToken();
                var (streamInfo, startedAt) = await _twitchService.CheckStreamStatus(twitchChannel, accessToken);
                if (streamInfo.Contains("не найден"))
                {
                    ViewBag.Message = streamInfo;
                }
                else
                {
                    if (await _twitchService.IsSubscribedChannel(GetChatIdFromSession(), twitchChannel, _dbContext))
                    {
                        ViewBag.Message = $"Вы уже подписаны на пользователя {twitchChannel}";
                    }
                    else
                    {
                        if (streamInfo.Contains("сейчас стримит"))
                        {
                            var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(GetTimeZoneFromSession());
                            var startedAtLocal = TimeZoneInfo.ConvertTimeFromUtc(startedAt.Value, userTimeZone);

                            ViewBag.StreamInfo = streamInfo;
                            ViewBag.StartedAt = startedAtLocal;
                            ViewBag.StreamUrl = $"https://www.twitch.tv/{twitchChannel}";
                            ViewBag.TwitchChannel = twitchChannel;
                        }
                        await _twitchService.SubscribeToTwitchChannel(GetChatIdFromSession(), twitchChannel, GetUsernameFromSession(), _dbContext);
                        ViewBag.Message = $"Подписка на {twitchChannel} успешно оформлена.";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Ошибка: {ex.Message}";
            }
            return View();
        }

		[HttpGet]
		public async Task<IActionResult> Unsubscribe()
		{
			ViewBag.Message = TempData["Message"];

			try
			{
				var chatId = GetChatIdFromSession();

				if (chatId == null)
				{
					throw new Exception("Время сессии истекло. Перезапустите приложение или войдите снова.");
				}

				var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.ChatId == chatId);
				if (user == null)
				{
					ViewBag.Message = "У вас нет активных подписок.";
					return View();
				}

				var subscriptions = await _twitchService.GetMyChannelSubscriptions(chatId, _dbContext);
				if (!subscriptions.Any())
				{
					ViewBag.Message = "У вас нет активных подписок.";
					return View();
				}

				ViewBag.Subscriptions = subscriptions;
			}
			catch (Exception ex)
			{
				ViewBag.Message = $"Ошибка: {ex.Message}";
			}

			return View();
		}

		[HttpPost]
		public async Task<IActionResult> ConfirmUnsubscriptions(List<string> twitchChannels)
		{
			try
			{
				if (twitchChannels == null || !twitchChannels.Any())
				{
					TempData["Message"] = "Вы не выбрали ни одного канала для отписки.";
					return RedirectToAction("Unsubscribe");
				}

				foreach (var twitchChannel in twitchChannels)
				{
					await _twitchService.UnsubscribeToTwitchChannel(GetChatIdFromSession(), twitchChannel, _dbContext);
				}

				TempData["Message"] = "Вы успешно отписались от выбранных каналов.";
			}
			catch (Exception ex)
			{
				TempData["Message"] = $"Ошибка: {ex.Message}";
			}

			return RedirectToAction("Unsubscribe");
		}

		public async Task<IActionResult> CheckStream(string twitchChannel)
        {
            if (twitchChannel != null)
            {
                var accessToken = await _twitchService.GetAccessToken();
                var (streamInfo, startedAt) = await _twitchService.CheckStreamStatus(twitchChannel, accessToken);

                if (startedAt.HasValue)
                {
                    var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(GetTimeZoneFromSession());
                    var startedAtLocal = TimeZoneInfo.ConvertTimeFromUtc(startedAt.Value, userTimeZone);

                    ViewBag.StartedAt = startedAtLocal;
                    ViewBag.StreamUrl = $"https://www.twitch.tv/{twitchChannel}";
                    ViewBag.TwitchChannel = twitchChannel;
                }

                ViewBag.StreamInfo = streamInfo;
            }
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ViewSubscriptions()
        {
			try
			{
				var chatId = GetChatIdFromSession();
				if (chatId == null)
				{
					ViewBag.Message = "Время сессии истекло. Вернитесь в главное меню или перезапустите приложение.";
					return View();
				}

				var subscriptions = await _twitchService.GetMyChannelSubscriptions(GetChatIdFromSession(), _dbContext);
				ViewBag.Subscriptions = subscriptions;
			}
			catch (Exception ex)
			{
				ViewBag.Message = $"Ошибка: {ex.Message}";
			}

			return View();
		}
    }
}
