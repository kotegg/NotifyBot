using Humanizer.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotifyBotMiniApp.Models;
using NotifyBotMiniApp.Services;
using Telegram.Bot.Types;
using User = NotifyBotMiniApp.Models.User;

namespace NotifyBotMiniApp.Controllers
{
    public class YouTubeController : BaseController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly YouTubeService _youTubeService;
        private static IConfiguration _configuration;

        public YouTubeController(ApplicationDbContext dbContext, YouTubeService youTubeService, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _youTubeService = youTubeService;
            _configuration = configuration;
        }

        public IActionResult YouTube()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Subscribe()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string channelName)
        {
            try
            {
                var channels = await _youTubeService.SearchChannels(channelName);

                if (channels == null || !channels.Any())
                {
                    ViewBag.Message = "Каналы с таким названием не найдены.";
                    return View();
                }

                if (channels.Count == 1)
                {
                    var channelId = channels.First().Id;
                    await _youTubeService.AddSubscription(GetChatIdFromSession(), GetUsernameFromSession(), channelId, channels.First().Title, _dbContext);
                    ViewBag.Message = $"Вы успешно подписались на канал: {channels.First().Title}";
                    return View();
                }

                ViewBag.Channels = channels;
                ViewBag.Message = "Выберите канал для подписки:";
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Ошибка: {ex.Message}";
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmSubscriptions(List<string> channelIds, Dictionary<string, string> channelTitles)
        {
            try
            {
                if (channelIds == null || !channelIds.Any())
                {
                    ViewBag.Message = "Вы не выбрали ни одного канала.";
                    return View("Subscribe");
                }

                foreach (var channelId in channelIds)
                {
                    var channelTitle = channelTitles[channelId];
                    await _youTubeService.AddSubscription(GetChatIdFromSession(), GetUsernameFromSession(), channelId, channelTitle, _dbContext);
                }

                ViewBag.Message = "Подписка на выбранные каналы успешно оформлена.";
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Ошибка: {ex.Message}";
            }

            return View("Subscribe");
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
                    ViewBag.Message = "Время сессии истекло. Вернитесь в главное меню или перезапустите приложение.";
                    return View();
                }

                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.ChatId == chatId);

                if (user == null)
                {
                    ViewBag.Message = "У вас нет подписок.";
                    return View();
                }

                var subscriptions = await _dbContext.YouTube_Subscriptions
                    .Where(sub => sub.UserId == user.UserId)
                    .ToListAsync();

                ViewBag.Subscriptions = subscriptions.Any() ? subscriptions : null;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Ошибка: {ex.Message}";
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmUnsubscriptions(List<string> channelIds)
        {
            try
            {
                if (channelIds == null || !channelIds.Any())
                {
					TempData["Message"] = "Вы не выбрали ни одного канала для отписки.";
                    return RedirectToAction("Unsubscribe");
                }

                foreach (var channelId in channelIds)
                {
                    await _youTubeService.Unsubscribe(GetChatIdFromSession(), channelId, _dbContext);
                }

				TempData["Message"] = "Вы успешно отписались от выбранных каналов.";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Ошибка: {ex.Message}";
            }

            return RedirectToAction("Unsubscribe");
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

				var subscriptions = await _youTubeService.GetMySubscriptions(GetChatIdFromSession(), _dbContext);
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
