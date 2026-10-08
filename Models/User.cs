using System.ComponentModel.DataAnnotations;

namespace chatWeb.Models
{
    public class User
    {
        [Key]
        public string Username { get; set; }
        public string Password { get; set; }
        public List<Message> Messages { get; set; } = new();
        public List<ChatParticipant> ChatParticipants { get; set; } = new();

    }
}