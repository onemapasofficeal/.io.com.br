namespace RoloxApp.Models
{
    public class RobloxGame
    {
        public long PlaceId { get; set; }
        public long UniverseId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }
        public string ThumbnailUrl { get; set; } = "";
        public string Creator { get; set; } = "Roblox Official";
        public string Publisher { get; set; } = "One Mapas Official";
    }
}
