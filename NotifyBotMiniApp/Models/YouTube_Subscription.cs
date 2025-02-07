namespace NotifyBotMiniApp.Models
{
    public class YouTube_Subscription
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string ChannelId { get; set; }

        public string ChannelName { get; set; }

        public DateTime SubscribedAt { get; set; }

        public User User { get; set; }
    }
}
