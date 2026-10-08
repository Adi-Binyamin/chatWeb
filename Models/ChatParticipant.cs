namespace chatWeb.Models
{
    public class ChatParticipant
    {
        public int ChatId { get; set; }
        public Chat Chat { get; set; }
        public string Username { get; set; } = "";
        public User User { get; set; }
    }
}