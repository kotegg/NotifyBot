namespace NotifyBotMiniApp.Models
{
    public class Twitch_Subscription
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string TwitchChannel { get; set; }

        public DateTime SubscribedAt { get; set; }

        public User User { get; set; }
    }
}
