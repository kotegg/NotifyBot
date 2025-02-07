using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotifyBotMiniApp.Models;

namespace NotifyBotMiniApp.Controllers
{
    public class BaseController : Controller
    {
        [HttpPost]
        public IActionResult SaveUserData([FromBody] UserData userData)
        {
            HttpContext.Session.SetInt32("ChatId", Convert.ToInt32(userData.ChatId));
            HttpContext.Session.SetString("Username", userData.Username);
            HttpContext.Session.SetString("TimeZone", userData.TimeZone);
            return Ok("User data saved.");
        }
        protected long? GetChatIdFromSession()
        {
            return HttpContext.Session.GetInt32("ChatId");
        }

        protected string? GetUsernameFromSession()
        {
            return HttpContext.Session.GetString("Username");
        }

        protected string? GetTimeZoneFromSession()
        {
            return HttpContext.Session.GetString("TimeZone");
        }
    }

    public class UserData
    {
        public long ChatId { get; set; }
        public string Username { get; set; }
        public string TimeZone { get; set; }
    }
}
