using System;

namespace RoloxApp.Models
{
    public class ChatMessage
    {
        public string Username { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsBanned { get; set; }
    }
}
