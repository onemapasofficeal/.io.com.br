namespace RoloxApp.Models
{
    public class RobloxUser
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
        public bool IsOnline { get; set; }
    }
}
