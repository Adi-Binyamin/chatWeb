namespace chatWeb.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public Chat Chat { get; set; }
        public string Username { get; set; } = "";
        public User User { get; set; }
        public string Text { get; set; } = "";
        public DateTime SentAt { get; set; }
    }
}