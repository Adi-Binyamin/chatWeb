namespace chatWeb.Models
{
    public class Chat
    {
        public int ChatId { get; set; }
        public List<Message> Messages { get; set; } = new();
        public List<ChatParticipant> Participants { get; set; } = new();
    }
}