using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RoloxStudio.Helpers;
using RoloxStudio.Models;

namespace RoloxStudio.Controls;

public class ChatMessageControl : Panel
{
    private readonly ChatMensagem _msg;
    private readonly bool         _isMine;

    private static readonly Font FName  = new("Segoe UI", 9f, FontStyle.Bold);
    private static readonly Font FText  = new("Segoe UI", 10f);
    private static readonly Font FTime  = new("Segoe UI", 7.5f);
    private static readonly Font FEmoji = new("Segoe UI Emoji", 13f);
    private static readonly Font FSys   = new("Segoe UI", 9f, FontStyle.Italic);

    private const int AV  = 36;
    private const int PAD = 10;
    private const int GAP = 6;

    public ChatMessageControl(ChatMensagem msg, bool isMine, int fixedWidth)
    {
        _msg    = msg;
        _isMine = isMine;

        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        BackColor = Color.Transparent;
        Width     = fixedWidth;
        Height    = ComputeHeight(fixedWidth);
    }

    private int ComputeHeight(int w)
    {
        if (_msg.EhSistema) return 26;

        int maxBubW = Math.Min(w - AV - GAP - 30, 420);
        if (maxBubW < 40) maxBubW = 200;

        using var bmp = new Bitmap(1, 1);
        using var g   = Graphics.FromImage(bmp);

        var msgSz = g.MeasureString(_msg.Conteudo, FText,
                        new SizeF(maxBubW - PAD * 2, 2000),
                        StringFormat.GenericDefault);

        int bubH = PAD
                 + (int)Math.Ceiling(FName.GetHeight()) + 2
                 + (int)Math.Ceiling(msgSz.Height)       + 2
                 + (int)Math.Ceiling(FTime.GetHeight())
                 + PAD;

        return Math.Max(bubH, AV) + 12;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (_msg.EhSistema) { DrawSystem(g); return; }
        DrawMessage(g);
    }

    private void DrawSystem(Graphics g)
    {
        using var b = new SolidBrush(ChatTheme.TextMuted);
        var fmt = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(_msg.Conteudo, FSys, b,
            new RectangleF(0, 0, Width, Height), fmt);
    }

    private void DrawMessage(Graphics g)
    {
        int maxBubW = Math.Min(Width - AV - GAP - 30, 420);
        if (maxBubW < 40) maxBubW = 200;

        var nameSz = g.MeasureString(_msg.AutorNome, FName);
        var msgSz  = g.MeasureString(_msg.Conteudo, FText,
                         new SizeF(maxBubW - PAD * 2, 2000),
                         StringFormat.GenericDefault);
        string timeStr = _msg.Timestamp.ToString("HH:mm");
        var timeSz = g.MeasureString(timeStr, FTime);

        int bubW = (int)Math.Ceiling(Math.Max(nameSz.Width,
                       Math.Max(msgSz.Width, timeSz.Width))) + PAD * 2 + 4;
        bubW = Math.Clamp(bubW, 80, maxBubW);

        int bubH = PAD
                 + (int)Math.Ceiling(nameSz.Height) + 2
                 + (int)Math.Ceiling(msgSz.Height)  + 2
                 + (int)Math.Ceiling(timeSz.Height)
                 + PAD;

        int avY, bubY, avX, bubX;
        avY = bubY = 6;

        if (_isMine)
        {
            avX  = Width - AV - 8;
            bubX = avX - GAP - bubW;
        }
        else
        {
            avX  = 8;
            bubX = avX + AV + GAP;
        }

        // Avatar
        string emoji = ChatTheme.Avatares[_msg.AutorAvatarIndex % ChatTheme.Avatares.Length];
        Color  avBg  = ChatTheme.GetUserColor(_msg.AutorAvatarIndex);
        using (var avBrush = new SolidBrush(avBg))
            g.FillEllipse(avBrush, avX, avY, AV, AV);
        var eSz = g.MeasureString(emoji, FEmoji);
        g.DrawString(emoji, FEmoji, Brushes.White,
            avX + (AV - eSz.Width)  / 2f,
            avY + (AV - eSz.Height) / 2f);

        // Balão
        Color bubColor = _isMine ? ChatTheme.AccentPurple : ChatTheme.BgMedium;
        FillRound(g, bubX, bubY, bubW, bubH, 12, bubColor);

        // Nome
        int ty = bubY + PAD;
        using (var nb = new SolidBrush(_msg.AutorCorNome))
            g.DrawString(_msg.AutorNome, FName, nb, bubX + PAD, ty);
        ty += (int)Math.Ceiling(nameSz.Height) + 2;

        // Texto
        g.DrawString(_msg.Conteudo, FText, Brushes.White,
            new RectangleF(bubX + PAD, ty, bubW - PAD * 2, msgSz.Height + 4));
        ty += (int)Math.Ceiling(msgSz.Height) + 2;

        // Hora
        using (var tb = new SolidBrush(ChatTheme.TextMuted))
            g.DrawString(timeStr, FTime, tb,
                bubX + bubW - (int)Math.Ceiling(timeSz.Width) - PAD, ty);
    }

    private static void FillRound(Graphics g, int x, int y, int w, int h, int r, Color c)
    {
        int d = r * 2;
        using var path = new GraphicsPath();
        path.AddArc(x,         y,         d, d, 180, 90);
        path.AddArc(x + w - d, y,         d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d,   0, 90);
        path.AddArc(x,         y + h - d, d, d,  90, 90);
        path.CloseFigure();
        using var brush = new SolidBrush(c);
        g.FillPath(brush, path);
    }
}
