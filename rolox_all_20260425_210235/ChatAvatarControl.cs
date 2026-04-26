using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RoloxStudio.Helpers;

namespace RoloxStudio.Controls;

public class ChatAvatarControl : Control
{
    public int    AvatarIndex     { get; set; } = 0;
    public string StatusText      { get; set; } = "Online";
    public bool   ShowStatus      { get; set; } = true;
    public Color  AvatarBackColor { get; set; } = Color.FromArgb(88, 101, 242);

    public ChatAvatarControl()
    {
        Size = new Size(44, 44);
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        int size       = Math.Min(Width, Height);
        int statusSize = ShowStatus ? 12 : 0;
        int avatarSize = size - (ShowStatus ? statusSize / 2 : 0);

        var rect = new Rectangle(0, 0, avatarSize - 1, avatarSize - 1);
        using var bgBrush = new SolidBrush(AvatarBackColor);
        g.FillEllipse(bgBrush, rect);

        string emoji = ChatTheme.Avatares[AvatarIndex % ChatTheme.Avatares.Length];
        using var emojiFont = new Font("Segoe UI Emoji", avatarSize * 0.42f);
        var emojiSize = g.MeasureString(emoji, emojiFont);
        g.DrawString(emoji, emojiFont, Brushes.White,
            (avatarSize - emojiSize.Width)  / 2f,
            (avatarSize - emojiSize.Height) / 2f);

        if (ShowStatus)
        {
            int sx = avatarSize - statusSize - 1;
            int sy = avatarSize - statusSize - 1;
            using var borderBrush = new SolidBrush(ChatTheme.BgDark);
            g.FillEllipse(borderBrush, sx - 2, sy - 2, statusSize + 4, statusSize + 4);
            using var statusBrush = new SolidBrush(ChatTheme.GetStatusColor(StatusText));
            g.FillEllipse(statusBrush, sx, sy, statusSize, statusSize);
        }
    }
}
