using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Drv3ThaiMod
{
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(10, 10, 18);
        public static readonly Color BgDeep = Color.FromArgb(6, 6, 11);
        public static readonly Color Card = Color.FromArgb(19, 19, 31);
        public static readonly Color CardHi = Color.FromArgb(30, 30, 47);
        public static readonly Color Text = Color.FromArgb(246, 245, 251);
        public static readonly Color Muted = Color.FromArgb(162, 159, 180);
        public static readonly Color Dim = Color.FromArgb(104, 101, 124);
        public static readonly Color Pink = Color.FromArgb(255, 43, 147);
        public static readonly Color PinkDeep = Color.FromArgb(186, 18, 102);
        public static readonly Color Cyan = Color.FromArgb(40, 229, 245);
        public static readonly Color Green = Color.FromArgb(84, 230, 155);
        public static readonly Color Yellow = Color.FromArgb(255, 200, 87);
        public static readonly Color Red = Color.FromArgb(255, 71, 94);

        public static Font Ui(float px) { return Ui(px, FontStyle.Regular); }
        public static Font Ui(float px, FontStyle style) { return new Font("Segoe UI", px, style, GraphicsUnit.Pixel); }
        public static Font Mono(float px) { return new Font("Consolas", px, FontStyle.Regular, GraphicsUnit.Pixel); }

        public static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) { path.AddRectangle(r); return path; }
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Diagonal hazard stripes used across the class-trial styled surfaces.
        public static void DrawStripes(Graphics g, Rectangle area, Color color, int spacing, int thickness, float offset)
        {
            if (area.Width <= 0 || area.Height <= 0) return;
            using (var pen = new Pen(color, thickness))
            {
                float span = area.Width + area.Height;
                for (float x = -area.Height + (offset % spacing); x < span; x += spacing)
                    g.DrawLine(pen, area.X + x, area.Bottom, area.X + x + area.Height, area.Y);
            }
        }

        public static Image LoadEmbedded(string name)
        {
            var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (stream == null) return null;
            using (stream) return Image.FromStream(stream);
        }

        // Window icon (taskbar / Alt-Tab) is separate from the exe's Win32 icon.
        public static Icon LoadAppIcon()
        {
            var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
            if (stream != null) using (stream) return new Icon(stream);
            try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch (Exception) { return null; }
        }

        // Translucent art watermark. `lift` raises the black areas so the sprite's dark
        // half does not disappear into the dark rail behind it.
        public static void DrawWatermark(Graphics g, Image image, RectangleF target, float opacity, float lift)
        {
            if (image == null) return;
            var matrix = new ColorMatrix(new[]
            {
                new[] { 1f, 0f, 0f, 0f, 0f },
                new[] { 0f, 1f, 0f, 0f, 0f },
                new[] { 0f, 0f, 1f, 0f, 0f },
                new[] { 0f, 0f, 0f, opacity, 0f },
                new[] { lift, lift, lift, 0f, 1f }
            });
            using (var attributes = new ImageAttributes())
            {
                attributes.SetColorMatrix(matrix);
                g.DrawImage(image,
                    new Rectangle((int)target.X, (int)target.Y, (int)target.Width, (int)target.Height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
        }
    }

    public class SmoothPanel : Panel
    {
        public SmoothPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }
    }

    /// Left rail: gradient, stripes, Monokuma watermark.
    public sealed class SidePanel : SmoothPanel
    {
        readonly Image bear = Theme.LoadEmbedded("monokuma.png");

        protected override void Dispose(bool disposing)
        {
            if (disposing && bear != null) bear.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (r.Width <= 0 || r.Height <= 0) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var brush = new LinearGradientBrush(r, Color.FromArgb(34, 14, 33), Color.FromArgb(8, 8, 16), 72f))
                g.FillRectangle(brush, r);

            Theme.DrawStripes(g, r, Color.FromArgb(14, 255, 255, 255), 26, 9, 0);

            if (bear != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                float bearH = 280f, bearW = bearH * bear.Width / bear.Height;
                Theme.DrawWatermark(g, bear, new RectangleF((r.Width - bearW) / 2f, r.Height - bearH - 54, bearW, bearH), 0.30f, 0.16f);
            }

            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(-160, r.Height * 0.06f, 420, 420);
                using (var pgb = new PathGradientBrush(glow))
                {
                    pgb.CenterColor = Color.FromArgb(46, 255, 43, 147);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, 255, 43, 147) };
                    g.FillPath(pgb, glow);
                }
            }

            using (var pen = new Pen(Color.FromArgb(70, 255, 43, 147), 2))
                g.DrawLine(pen, r.Right - 1, 0, r.Right - 1, r.Bottom);
        }
    }

    /// Main surface: deep background, faint dot grid, corner accents.
    public sealed class StagePanel : SmoothPanel
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (r.Width <= 0 || r.Height <= 0) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var brush = new LinearGradientBrush(r, Color.FromArgb(14, 13, 22), Theme.BgDeep, 115f))
                g.FillRectangle(brush, r);

            using (var dot = new SolidBrush(Color.FromArgb(16, 255, 255, 255)))
                for (int y = 12; y < r.Height; y += 34)
                    for (int x = 12; x < r.Width; x += 34)
                        g.FillRectangle(dot, x, y, 2, 2);

            using (var pen = new Pen(Color.FromArgb(52, 40, 229, 245), 2))
            {
                g.DrawLine(pen, r.Right - 118, r.Y + 14, r.Right - 14, r.Y + 14);
                g.DrawLine(pen, r.Right - 14, r.Y + 14, r.Right - 14, r.Y + 96);
                g.DrawLine(pen, r.X + 14, r.Bottom - 14, r.X + 104, r.Bottom - 14);
            }
        }
    }

    /// Rounded card with a neon edge.
    public sealed class NeonCard : SmoothPanel
    {
        public Color Accent = Theme.Pink;
        public int Radius = 10;

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (r.Width <= 2 || r.Height <= 2) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);

            using (var path = Theme.Rounded(bounds, Radius))
            {
                using (var fill = new LinearGradientBrush(bounds, Theme.CardHi, Theme.Card, 90f))
                    g.FillPath(fill, path);
                using (var pen = new Pen(Color.FromArgb(46, Accent), 1))
                    g.DrawPath(pen, path);
            }

            using (var pen = new Pen(Accent, 3))
                g.DrawLine(pen, bounds.X + Radius, bounds.Y + 1, bounds.X + Radius + 58, bounds.Y + 1);
        }
    }

    /// Truth-Bullet styled progress bar with animated hazard stripes.
    public sealed class BulletProgress : SmoothPanel
    {
        int value;
        float phase;
        readonly Timer animation;

        public BulletProgress()
        {
            animation = new Timer();
            animation.Interval = 33;
            animation.Tick += delegate { phase -= 1.4f; Invalidate(); };
        }

        public int Value
        {
            get { return value; }
            set
            {
                int clamped = Math.Max(0, Math.Min(100, value));
                if (clamped == this.value) return;
                this.value = clamped;
                Invalidate();
            }
        }

        public bool Animating
        {
            get { return animation.Enabled; }
            set { if (value) animation.Start(); else animation.Stop(); Invalidate(); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) animation.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (r.Width <= 4 || r.Height <= 4) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var track = new Rectangle(r.X, r.Y + 2, r.Width - 1, r.Height - 5);
            int radius = track.Height / 2;

            using (var path = Theme.Rounded(track, radius))
            {
                using (var brush = new SolidBrush(Color.FromArgb(255, 15, 15, 24)))
                    g.FillPath(brush, path);
                using (var pen = new Pen(Color.FromArgb(70, 255, 255, 255), 1))
                    g.DrawPath(pen, path);
            }

            int filled = (int)(track.Width * (value / 100f));
            if (filled > 3)
            {
                var fillRect = new Rectangle(track.X, track.Y, filled, track.Height);
                using (var path = Theme.Rounded(fillRect, radius))
                {
                    using (var brush = new LinearGradientBrush(
                        new Rectangle(track.X, track.Y, Math.Max(track.Width, 1), track.Height),
                        Theme.Pink, Theme.Cyan, 0f))
                        g.FillPath(brush, path);

                    var saved = g.Save();
                    g.SetClip(path, CombineMode.Replace);
                    Theme.DrawStripes(g, fillRect, Color.FromArgb(58, 255, 255, 255), 22, 8, phase);
                    g.Restore(saved);
                }

                using (var pen = new Pen(Color.FromArgb(150, 255, 255, 255), 2))
                    g.DrawLine(pen, fillRect.Right - 2, track.Y + 3, fillRect.Right - 2, track.Bottom - 3);
            }
        }
    }

    /// Angular Danganronpa-style button with hover / press feedback.
    public sealed class DrButton : Button
    {
        public Color Accent = Theme.Pink;
        public bool Filled = true;
        const int Skew = 12;
        bool hovered, pressed;

        public DrButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnEnabledChanged(e); }

        GraphicsPath Shape(Rectangle r)
        {
            var path = new GraphicsPath();
            path.AddPolygon(new[]
            {
                new Point(r.X + Skew, r.Y),
                new Point(r.Right, r.Y),
                new Point(r.Right - Skew, r.Bottom),
                new Point(r.X, r.Bottom)
            });
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (r.Width <= 4 || r.Height <= 4) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // The angled shape leaves bare corners; paint the parent's solid colour first
            // so nothing from the previous frame shows through them.
            if (Parent != null && Parent.BackColor.A == 255)
                using (var backdrop = new SolidBrush(Parent.BackColor))
                    g.FillRectangle(backdrop, r);

            int shift = pressed ? 1 : 0;
            var body = new Rectangle(r.X, r.Y + shift, r.Width - 1, r.Height - 2);

            Color accent = Enabled ? Accent : Color.FromArgb(70, 70, 88);
            Color textColor = Enabled ? (Filled ? Color.White : accent) : Color.FromArgb(120, 118, 136);

            using (var path = Shape(body))
            {
                if (Filled)
                {
                    Color top = hovered && Enabled ? ControlPaint.Light(accent, 0.18f) : accent;
                    Color bottom = Enabled ? ControlPaint.Dark(accent, 0.22f) : Color.FromArgb(52, 52, 66);
                    using (var brush = new LinearGradientBrush(body, top, bottom, 90f))
                        g.FillPath(brush, path);
                }
                else
                {
                    Color fill = hovered && Enabled ? Color.FromArgb(46, accent) : Color.FromArgb(255, 26, 26, 41);
                    using (var brush = new SolidBrush(fill))
                        g.FillPath(brush, path);
                }

                using (var pen = new Pen(Enabled ? Color.FromArgb(hovered ? 235 : 150, accent) : Color.FromArgb(90, 90, 110), 2))
                    g.DrawPath(pen, path);

                if (hovered && Enabled)
                    using (var pen = new Pen(Color.FromArgb(70, Color.White), 1))
                        g.DrawPath(pen, Shape(new Rectangle(body.X + 3, body.Y + 3, body.Width - 6, body.Height - 6)));
            }

            TextRenderer.DrawText(g, Text, Font, body, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    /// Embedded artwork with a soft pink halo behind it.
    public sealed class LogoPanel : SmoothPanel
    {
        readonly Image logo;

        public LogoPanel() : this("logo.png") { }

        public LogoPanel(string resourceName)
        {
            BackColor = Color.Transparent;
            logo = Theme.LoadEmbedded(resourceName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && logo != null) logo.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (r.Width <= 8 || r.Height <= 8 || logo == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            using (var halo = new GraphicsPath())
            {
                halo.AddEllipse(-r.Width * 0.18f, r.Height * 0.04f, r.Width * 1.36f, r.Height * 0.92f);
                using (var pgb = new PathGradientBrush(halo))
                {
                    pgb.CenterColor = Color.FromArgb(70, 255, 43, 147);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, 255, 43, 147) };
                    g.FillPath(pgb, halo);
                }
            }

            float scale = Math.Min((float)r.Width / logo.Width, (float)r.Height / logo.Height);
            float w = logo.Width * scale, h = logo.Height * scale;
            g.DrawImage(logo, (r.Width - w) / 2f, (r.Height - h) / 2f, w, h);
        }
    }

    /// Emblem: rotating trial ring + V3 sigil.
    public sealed class EmblemPanel : SmoothPanel
    {
        float angle;
        readonly Timer spin;

        public EmblemPanel()
        {
            BackColor = Color.Transparent;
            spin = new Timer();
            spin.Interval = 40;
            spin.Tick += delegate { angle = (angle + 0.55f) % 360f; Invalidate(); };
            spin.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) spin.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (r.Width <= 8 || r.Height <= 8) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = r.Width / 2f, cy = r.Height / 2f;
            float outer = Math.Min(r.Width, r.Height) / 2f - 4f;

            using (var glowPath = new GraphicsPath())
            {
                glowPath.AddEllipse(cx - outer - 14, cy - outer - 14, (outer + 14) * 2, (outer + 14) * 2);
                using (var pgb = new PathGradientBrush(glowPath))
                {
                    pgb.CenterColor = Color.FromArgb(90, 255, 43, 147);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, 255, 43, 147) };
                    g.FillPath(pgb, glowPath);
                }
            }

            var saved = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(angle);
            using (var pen = new Pen(Color.FromArgb(150, Theme.Cyan), 2))
                for (int i = 0; i < 24; i++)
                {
                    double a = Math.PI * 2 * i / 24;
                    float inner = (i % 3 == 0) ? outer - 13 : outer - 6;
                    g.DrawLine(pen,
                        (float)(Math.Cos(a) * inner), (float)(Math.Sin(a) * inner),
                        (float)(Math.Cos(a) * outer), (float)(Math.Sin(a) * outer));
                }
            g.Restore(saved);

            using (var pen = new Pen(Theme.Pink, 5))
                g.DrawEllipse(pen, cx - outer + 16, cy - outer + 16, (outer - 16) * 2, (outer - 16) * 2);
            using (var pen = new Pen(Color.FromArgb(190, Theme.Cyan), 2))
                g.DrawEllipse(pen, cx - outer + 26, cy - outer + 26, (outer - 26) * 2, (outer - 26) * 2);

            float triR = outer - 40;
            var tri = new[]
            {
                new PointF(cx, cy - triR),
                new PointF(cx + triR * 0.90f, cy + triR * 0.62f),
                new PointF(cx - triR * 0.90f, cy + triR * 0.62f)
            };
            using (var brush = new LinearGradientBrush(new RectangleF(cx - triR, cy - triR, triR * 2, triR * 2), Theme.Pink, Theme.PinkDeep, 90f))
                g.FillPolygon(brush, tri);
            using (var pen = new Pen(Color.FromArgb(235, 255, 255, 255), 2))
                g.DrawPolygon(pen, tri);

            using (var font = new Font("Arial Black", outer * 0.44f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                var size = g.MeasureString("V3", font);
                g.DrawString("V3", font, Brushes.White, cx - size.Width / 2f, cy - size.Height / 2f + triR * 0.16f);
            }
        }
    }

    /// Custom window chrome so the frame matches the dark trial theme.
    public sealed class TitleBar : SmoothPanel
    {
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        const int WmNcLButtonDown = 0xA1;
        const int HtCaption = 0x2;

        readonly Form owner;
        Rectangle closeRect, minimizeRect;
        int hotButton = -1;

        public TitleBar(Form form)
        {
            owner = form;
            BackColor = Color.Transparent;
            Height = 44;
        }

        void Layout()
        {
            closeRect = new Rectangle(Width - 52, 0, 52, Height);
            minimizeRect = new Rectangle(Width - 104, 0, 52, Height);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Layout();
            int hot = closeRect.Contains(e.Location) ? 0 : (minimizeRect.Contains(e.Location) ? 1 : -1);
            if (hot != hotButton) { hotButton = hot; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (hotButton != -1) { hotButton = -1; Invalidate(); }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Layout();
            if (e.Button == MouseButtons.Left)
            {
                if (closeRect.Contains(e.Location)) { owner.Close(); return; }
                if (minimizeRect.Contains(e.Location)) { owner.WindowState = FormWindowState.Minimized; return; }
                ReleaseCapture();
                SendMessage(owner.Handle, WmNcLButtonDown, HtCaption, 0);
            }
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (r.Width <= 0 || r.Height <= 0) return;
            Layout();
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var brush = new LinearGradientBrush(r, Color.FromArgb(26, 12, 26), Color.FromArgb(10, 10, 18), 0f))
                g.FillRectangle(brush, r);
            Theme.DrawStripes(g, r, Color.FromArgb(12, 255, 255, 255), 22, 7, 0);

            using (var pen = new Pen(Color.FromArgb(120, Theme.Pink), 1))
                g.DrawLine(pen, 0, r.Bottom - 1, r.Width, r.Bottom - 1);

            using (var brush = new SolidBrush(Theme.Pink))
                g.FillPolygon(brush, new[] { new Point(20, 14), new Point(26, 14), new Point(21, 30), new Point(15, 30) });

            TextRenderer.DrawText(g, "DANGANRONPA V3  ·  THAI MOD INSTALLER", Theme.Ui(15, FontStyle.Bold),
                new Rectangle(36, 0, r.Width - 160, r.Height), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            if (hotButton == 0)
                using (var brush = new SolidBrush(Color.FromArgb(200, 216, 30, 52)))
                    g.FillRectangle(brush, closeRect);
            else if (hotButton == 1)
                using (var brush = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                    g.FillRectangle(brush, minimizeRect);

            using (var pen = new Pen(hotButton == 0 ? Color.White : Theme.Muted, 2))
            {
                int cx = closeRect.X + closeRect.Width / 2, cy = closeRect.Y + closeRect.Height / 2;
                g.DrawLine(pen, cx - 6, cy - 6, cx + 6, cy + 6);
                g.DrawLine(pen, cx + 6, cy - 6, cx - 6, cy + 6);
            }
            using (var pen = new Pen(hotButton == 1 ? Color.White : Theme.Muted, 2))
            {
                int cx = minimizeRect.X + minimizeRect.Width / 2, cy = minimizeRect.Y + minimizeRect.Height / 2;
                g.DrawLine(pen, cx - 6, cy + 4, cx + 6, cy + 4);
            }
        }
    }

    /// Pill-shaped status badge.
    public sealed class StatusBadge : SmoothPanel
    {
        string label = "";
        Color accent = Theme.Yellow;

        public StatusBadge() { BackColor = Color.Transparent; }

        public void Set(string text, Color color)
        {
            label = text;
            accent = color;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            if (r.Width <= 4 || r.Height <= 4) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var pill = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            using (var path = Theme.Rounded(pill, pill.Height / 2))
            {
                using (var brush = new SolidBrush(Color.FromArgb(38, accent)))
                    g.FillPath(brush, path);
                using (var pen = new Pen(Color.FromArgb(130, accent), 1))
                    g.DrawPath(pen, path);
            }

            float dotR = 5f;
            float dotX = pill.X + 16, dotY = pill.Y + pill.Height / 2f;
            using (var glowPath = new GraphicsPath())
            {
                glowPath.AddEllipse(dotX - dotR * 3, dotY - dotR * 3, dotR * 6, dotR * 6);
                using (var pgb = new PathGradientBrush(glowPath))
                {
                    pgb.CenterColor = Color.FromArgb(120, accent);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, accent) };
                    g.FillPath(pgb, glowPath);
                }
            }
            using (var brush = new SolidBrush(accent))
                g.FillEllipse(brush, dotX - dotR, dotY - dotR, dotR * 2, dotR * 2);

            var textRect = new Rectangle(pill.X + 30, pill.Y, pill.Width - 40, pill.Height);
            TextRenderer.DrawText(g, label, Font, textRect, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }

    /// Themed replacement for MessageBox, with a dimmed backdrop over the installer.
    public sealed class DrDialog : Form
    {
        public enum Kind { Question, Info, Warning, Error }

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        readonly Kind kind;
        readonly string heading;
        readonly string body;
        readonly Font bodyFont = Theme.Ui(20);
        readonly Image bear;

        DrDialog(string heading, string body, Kind kind, bool yesNo, string confirmText)
        {
            this.kind = kind;
            this.heading = heading;
            this.body = body;

            string art;
            switch (kind)
            {
                case Kind.Info: art = "monokuma_info.png"; break;
                case Kind.Warning: art = "monokuma_warning.png"; break;
                case Kind.Error: art = "monokuma_error.png"; break;
                default: art = "monokuma_question.png"; break;
            }
            bear = Theme.LoadEmbedded(art);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(22, 22, 35);
            ForeColor = Theme.Text;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            int width = 640;
            var textSize = TextRenderer.MeasureText(body, bodyFont, new Size(width - 168, 600), TextFormatFlags.WordBreak);
            int height = Math.Max(236, 150 + textSize.Height + 86);
            ClientSize = new Size(width, height);

            var confirm = new DrButton
            {
                Text = confirmText,
                Accent = kind == Kind.Error ? Theme.Red : Theme.Pink,
                Filled = true,
                Font = Theme.Ui(20, FontStyle.Bold),
                Size = new Size(yesNo ? 172 : 148, 52),
                DialogResult = DialogResult.Yes
            };
            confirm.Location = new Point(width - 28 - confirm.Width, height - 28 - confirm.Height);
            Controls.Add(confirm);
            AcceptButton = confirm;

            if (yesNo)
            {
                var cancel = new DrButton
                {
                    Text = "ยกเลิก",
                    Accent = Theme.Muted,
                    Filled = false,
                    Font = Theme.Ui(20, FontStyle.Bold),
                    Size = new Size(140, 52),
                    DialogResult = DialogResult.No
                };
                cancel.Location = new Point(confirm.Left - 16 - cancel.Width, confirm.Top);
                Controls.Add(cancel);
                CancelButton = cancel;
            }
            else
            {
                CancelButton = confirm;
            }

            MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && e.Y < 56)
                {
                    ReleaseCapture();
                    SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bodyFont.Dispose();
                if (bear != null) bear.Dispose();
            }
            base.Dispose(disposing);
        }

        Color Accent
        {
            get
            {
                switch (kind)
                {
                    case Kind.Info: return Theme.Green;
                    case Kind.Warning: return Theme.Yellow;
                    case Kind.Error: return Theme.Red;
                    default: return Theme.Cyan;
                }
            }
        }

        string Glyph
        {
            get
            {
                switch (kind)
                {
                    case Kind.Info: return "✓";
                    case Kind.Warning: return "!";
                    case Kind.Error: return "✕";
                    default: return "?";
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var r = ClientRectangle;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Color accent = Accent;

            using (var brush = new LinearGradientBrush(r, Theme.CardHi, Color.FromArgb(14, 14, 23), 108f))
                g.FillRectangle(brush, r);

            var header = new Rectangle(0, 0, r.Width, 56);
            using (var brush = new LinearGradientBrush(header, Color.FromArgb(30, 14, 30), Color.FromArgb(16, 16, 26), 0f))
                g.FillRectangle(brush, header);
            Theme.DrawStripes(g, header, Color.FromArgb(14, 255, 255, 255), 22, 7, 0);
            using (var pen = new Pen(Color.FromArgb(150, accent), 1))
                g.DrawLine(pen, 0, header.Bottom, r.Width, header.Bottom);
            using (var brush = new SolidBrush(accent))
                g.FillPolygon(brush, new[] { new Point(26, 18), new Point(32, 18), new Point(27, 38), new Point(21, 38) });
            TextRenderer.DrawText(g, heading, Theme.Ui(19, FontStyle.Bold), new Rectangle(44, 0, r.Width - 80, header.Height),
                Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            var badge = new Rectangle(34, 96, 74, 74);
            using (var halo = new GraphicsPath())
            {
                halo.AddEllipse(badge.X - 22, badge.Y - 14, badge.Width + 44, badge.Height + 52);
                using (var pgb = new PathGradientBrush(halo))
                {
                    pgb.CenterColor = Color.FromArgb(bear != null ? 120 : 90, accent);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, accent) };
                    g.FillPath(pgb, halo);
                }
            }

            if (bear != null)
            {
                // Art varies between half-body and full-body sprites, so fit it to a box.
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                var area = new Rectangle(24, 84, 108, 138);
                float fit = Math.Min(area.Width / (float)bear.Width, area.Height / (float)bear.Height);
                float bw = bear.Width * fit, bh = bear.Height * fit;
                g.DrawImage(bear, area.X + (area.Width - bw) / 2f, area.Y + (area.Height - bh) / 2f, bw, bh);
            }
            else
            {
                using (var brush = new SolidBrush(Color.FromArgb(46, accent)))
                    g.FillEllipse(brush, badge);
                using (var pen = new Pen(accent, 3))
                    g.DrawEllipse(pen, badge);
                using (var font = new Font("Segoe UI", 38, FontStyle.Bold, GraphicsUnit.Pixel))
                    TextRenderer.DrawText(g, Glyph, font, badge, accent,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            var textRect = new Rectangle(140, 92, r.Width - 172, r.Height - 92 - 100);
            TextRenderer.DrawText(g, body, bodyFont, textRect, Theme.Text,
                TextFormatFlags.WordBreak | TextFormatFlags.Left | TextFormatFlags.NoPadding);

            using (var pen = new Pen(Color.FromArgb(170, accent), 1))
                g.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
        }

        public static DialogResult Show(Form owner, string heading, string body, Kind kind, bool yesNo, string confirmText)
        {
            using (var backdrop = new Form())
            {
                backdrop.FormBorderStyle = FormBorderStyle.None;
                backdrop.StartPosition = FormStartPosition.Manual;
                backdrop.ShowInTaskbar = false;
                backdrop.BackColor = Color.Black;
                backdrop.Opacity = 0.55;
                backdrop.Bounds = owner.Bounds;
                backdrop.Show(owner);

                using (var dialog = new DrDialog(heading, body, kind, yesNo, confirmText))
                {
                    dialog.StartPosition = FormStartPosition.Manual;
                    dialog.Location = new Point(
                        owner.Left + (owner.Width - dialog.Width) / 2,
                        owner.Top + (owner.Height - dialog.Height) / 2);
                    var result = dialog.ShowDialog(owner);
                    backdrop.Hide();
                    return result;
                }
            }
        }

        public static void Info(Form owner, string heading, string body) { Show(owner, heading, body, Kind.Info, false, "ตกลง"); }
        public static void Error(Form owner, string heading, string body) { Show(owner, heading, body, Kind.Error, false, "ปิด"); }
        public static void Warn(Form owner, string heading, string body) { Show(owner, heading, body, Kind.Warning, false, "รับทราบ"); }
    }

    public sealed class InstallerForm : Form
    {
        readonly TextBox gamePath = new TextBox();
        readonly StatusBadge statusBadge = new StatusBadge();
        readonly Label phaseText = new Label();
        readonly Label percentText = new Label();
        readonly BulletProgress progress = new BulletProgress();
        readonly RichTextBox log = new RichTextBox();
        readonly DrButton browseButton = new DrButton();
        readonly DrButton installButton = new DrButton();
        readonly DrButton uninstallButton = new DrButton();
        Process worker;
        System.Windows.Forms.Timer progressTimer;
        string progressFile;
        int progressCharacters;

        public InstallerForm()
        {
            Text = "Danganronpa V3 Thai Mod — Installer";
            ClientSize = new Size(1280, 860);
            MinimumSize = new Size(1280, 860);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Ui(18f);
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            var appIcon = Theme.LoadAppIcon();
            if (appIcon != null) Icon = appIcon;
            BuildUi();
            gamePath.Text = FindDefaultGame();
            gamePath.SelectionStart = 0;
            gamePath.SelectionLength = 0;
            ActiveControl = installButton;
            RefreshState();
            FormClosing += OnFormClosing;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Color.FromArgb(150, Theme.Pink), 1))
                e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ShowLogPlaceholder();
        }

        void BuildUi()
        {
            const int TitleH = 44;
            int bodyH = ClientSize.Height - TitleH;

            var stage = new StagePanel { Location = new Point(400, TitleH), Size = new Size(880, bodyH) };
            Controls.Add(stage);

            var left = new SidePanel { Location = new Point(0, TitleH), Size = new Size(400, bodyH) };
            Controls.Add(left);

            var titleBar = new TitleBar(this) { Location = new Point(0, 0), Size = new Size(ClientSize.Width, TitleH) };
            Controls.Add(titleBar);

            left.Controls.Add(new LogoPanel { Location = new Point(8, 44), Size = new Size(384, 122) });

            AddLabel(left, "THAI MOD", 0, 172, 400, 48, 36, FontStyle.Bold, Theme.Text, ContentAlignment.MiddleCenter);
            AddLabel(left, "ม็อดไทยแปลโดย", 0, 222, 400, 26, 18, FontStyle.Bold, Theme.Cyan, ContentAlignment.MiddleCenter);
            left.Controls.Add(new LogoPanel("translator.png") { Location = new Point(134, 252), Size = new Size(132, 140) });

            var accent = new SmoothPanel { Location = new Point(167, 402), Size = new Size(66, 5), BackColor = Color.Transparent };
            accent.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new LinearGradientBrush(accent.ClientRectangle, Theme.Pink, Theme.Cyan, 0f))
                    e.Graphics.FillPolygon(brush, new[]
                    {
                        new Point(4, 0), new Point(66, 0), new Point(62, 5), new Point(0, 5)
                    });
            };
            left.Controls.Add(accent);

            AddLabel(left, "ติดตั้งภาษาไทยอย่างปลอดภัย\r\nพร้อมสำรองไฟล์ต้นฉบับอัตโนมัติ", 50, 420, 300, 66, 19, FontStyle.Regular, Theme.Muted, ContentAlignment.MiddleCenter);

            var main = stage;

            var titleSlash = new SmoothPanel { Location = new Point(42, 20), Size = new Size(8, 46), BackColor = Color.Transparent };
            titleSlash.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new LinearGradientBrush(titleSlash.ClientRectangle, Theme.Pink, Theme.PinkDeep, 90f))
                    e.Graphics.FillPolygon(brush, new[] { new Point(4, 0), new Point(8, 0), new Point(4, 46), new Point(0, 46) });
            };
            main.Controls.Add(titleSlash);

            AddLabel(main, "ติดตั้งม็อดภาษาไทย", 64, 14, 780, 58, 40, FontStyle.Bold, Theme.Text, ContentAlignment.MiddleLeft);
            AddLabel(main, "DANGANRONPA V3: KILLING HARMONY  ·  STEAM", 66, 74, 780, 28, 16, FontStyle.Bold, Theme.Dim, ContentAlignment.MiddleLeft);

            var gameCard = new NeonCard { Location = new Point(42, 122), Size = new Size(800, 186), Accent = Theme.Pink };
            main.Controls.Add(gameCard);

            AddLabel(gameCard, "ตำแหน่งเกม", 26, 18, 300, 32, 21, FontStyle.Bold, Theme.Text, ContentAlignment.MiddleLeft);

            var pathWell = new SmoothPanel { Location = new Point(26, 60), Size = new Size(566, 44), BackColor = Color.Transparent };
            pathWell.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pathWell.Width - 1, pathWell.Height - 1);
                using (var path = Theme.Rounded(rect, 6))
                {
                    using (var brush = new SolidBrush(Color.FromArgb(255, 12, 12, 20)))
                        e.Graphics.FillPath(brush, path);
                    using (var pen = new Pen(Color.FromArgb(90, Theme.Cyan), 1))
                        e.Graphics.DrawPath(pen, path);
                }
            };
            gameCard.Controls.Add(pathWell);

            gamePath.Location = new Point(12, 11);
            gamePath.Size = new Size(542, 24);
            gamePath.BackColor = Color.FromArgb(12, 12, 20);
            gamePath.ForeColor = Theme.Text;
            gamePath.BorderStyle = BorderStyle.None;
            gamePath.Font = Theme.Ui(19);
            gamePath.TextChanged += delegate { RefreshState(); };
            pathWell.Controls.Add(gamePath);

            browseButton.Text = "เลือกโฟลเดอร์";
            browseButton.Accent = Theme.Cyan;
            browseButton.Filled = false;
            browseButton.Font = Theme.Ui(19, FontStyle.Bold);
            browseButton.Location = new Point(614, 58);
            browseButton.Size = new Size(160, 48);
            browseButton.Click += Browse;
            gameCard.Controls.Add(browseButton);

            statusBadge.Location = new Point(26, 124);
            statusBadge.Size = new Size(540, 38);
            statusBadge.Font = Theme.Ui(18);
            gameCard.Controls.Add(statusBadge);

            phaseText.Location = new Point(44, 336);
            phaseText.Size = new Size(640, 34);
            phaseText.BackColor = Color.Transparent;
            phaseText.ForeColor = Theme.Text;
            phaseText.Font = Theme.Ui(21, FontStyle.Bold);
            phaseText.Text = "พร้อมทำงาน";
            main.Controls.Add(phaseText);

            percentText.Location = new Point(702, 334);
            percentText.Size = new Size(140, 36);
            percentText.BackColor = Color.Transparent;
            percentText.TextAlign = ContentAlignment.MiddleRight;
            percentText.Font = new Font("Consolas", 26, FontStyle.Bold, GraphicsUnit.Pixel);
            percentText.ForeColor = Theme.Cyan;
            percentText.Text = "0%";
            main.Controls.Add(percentText);

            progress.Location = new Point(42, 380);
            progress.Size = new Size(800, 26);
            main.Controls.Add(progress);

            var logCard = new NeonCard { Location = new Point(42, 424), Size = new Size(800, 300), Accent = Theme.Cyan, Radius = 8 };
            main.Controls.Add(logCard);

            log.Location = new Point(14, 14);
            log.Size = new Size(772, 272);
            log.BackColor = Color.FromArgb(9, 9, 15);
            log.ForeColor = Color.FromArgb(198, 196, 212);
            log.BorderStyle = BorderStyle.None;
            log.ReadOnly = true;
            log.ScrollBars = RichTextBoxScrollBars.None;
            log.Font = Theme.Mono(17);
            logCard.Controls.Add(log);

            uninstallButton.Text = "ถอนการติดตั้ง";
            uninstallButton.Accent = Theme.Muted;
            uninstallButton.Filled = false;
            uninstallButton.Font = Theme.Ui(20, FontStyle.Bold);
            uninstallButton.Location = new Point(42, 744);
            uninstallButton.Size = new Size(232, 56);
            uninstallButton.Click += ConfirmUninstall;
            main.Controls.Add(uninstallButton);

            installButton.Text = "ติดตั้งภาษาไทย";
            installButton.Accent = Theme.Pink;
            installButton.Filled = true;
            installButton.Font = Theme.Ui(21, FontStyle.Bold);
            installButton.Location = new Point(596, 740);
            installButton.Size = new Size(246, 60);
            installButton.Click += ConfirmInstall;
            main.Controls.Add(installButton);
        }

        static Label AddLabel(Control parent, string text, int x, int y, int w, int h, float size, FontStyle style, Color color, ContentAlignment align)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                ForeColor = color,
                BackColor = Color.Transparent,
                Font = Theme.Ui(size, style),
                TextAlign = align
            };
            parent.Controls.Add(label);
            return label;
        }

        string FindDefaultGame()
        {
            string[] candidates = {
                @"C:\Program Files (x86)\Steam\steamapps\common\Danganronpa V3 Killing Harmony",
                @"C:\Program Files\Steam\steamapps\common\Danganronpa V3 Killing Harmony"
            };
            foreach (string p in candidates) if (Directory.Exists(Path.Combine(p, "data", "win"))) return p;
            return "";
        }

        string ValidGamePath()
        {
            string path = gamePath.Text.Trim().Trim('"');
            return Directory.Exists(Path.Combine(path, "data", "win")) ? path : null;
        }

        void RefreshState()
        {
            if (worker != null && !worker.HasExited) return;
            string game = ValidGamePath();
            if (game == null) { SetStatus("ยังไม่พบโฟลเดอร์เกม", Theme.Red); installButton.Enabled = uninstallButton.Enabled = false; return; }
            string backup = Path.Combine(game, "thai_mod_original_cpk_backup");
            if (File.Exists(Path.Combine(backup, "thai_mod_install_manifest.json")))
            { SetStatus("ติดตั้งม็อดภาษาไทยแล้ว", Theme.Green); installButton.Enabled = false; uninstallButton.Enabled = true; return; }
            string win = Path.Combine(game, "data", "win");
            bool clean = File.Exists(Path.Combine(win, "partition_data_win_us.cpk")) && File.Exists(Path.Combine(win, "partition_resident_win.cpk"));
            if (clean) { SetStatus("พบเกม พร้อมติดตั้ง", Theme.Green); installButton.Enabled = true; uninstallButton.Enabled = false; }
            else { SetStatus("ไฟล์เกมไม่ครบหรือมีม็อดแบบ manual อยู่", Theme.Yellow); installButton.Enabled = uninstallButton.Enabled = false; }
        }

        void SetStatus(string text, Color color) { statusBadge.Set(text, color); }

        void Browse(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = "เลือกโฟลเดอร์ Danganronpa V3 Killing Harmony", ShowNewFolderButton = false })
                if (dialog.ShowDialog(this) == DialogResult.OK) gamePath.Text = dialog.SelectedPath;
        }

        void ConfirmInstall(object sender, EventArgs e)
        {
            var answer = DrDialog.Show(this, "ยืนยันการติดตั้ง",
                "ปิดเกมแล้วหรือยัง?\r\n\r\nตัวติดตั้งต้องใช้พื้นที่ว่างประมาณ 12 GB และจะสำรอง CPK ต้นฉบับให้โดยอัตโนมัติ",
                DrDialog.Kind.Question, true, "เริ่มติดตั้ง");
            if (answer == DialogResult.Yes) StartWorker(false);
        }

        void ConfirmUninstall(object sender, EventArgs e)
        {
            var answer = DrDialog.Show(this, "ยืนยันการถอนการติดตั้ง",
                "ต้องการถอนม็อดภาษาไทยและคืนไฟล์เกมต้นฉบับใช่ไหม?\r\n\r\n\r\nไฟล์ CPK ที่สำรองไว้จะถูกนำกลับเข้าโฟลเดอร์เกม",
                DrDialog.Kind.Warning, true, "ถอนการติดตั้ง");
            if (answer == DialogResult.Yes) StartWorker(true);
        }

        void StartWorker(bool uninstall)
        {
            string game = ValidGamePath(); if (game == null) return;
            string backend = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "DRV3ThaiBackend.exe");
            if (!File.Exists(backend)) { DrDialog.Error(this, "แพ็กไม่สมบูรณ์", "ไม่พบไฟล์ bin\\DRV3ThaiBackend.exe\r\n\r\nกรุณาแตกไฟล์ ZIP ทั้งโฟลเดอร์ก่อนเปิดโปรแกรม"); return; }
            log.Clear(); AppendLog("กรุณาอย่าปิดหน้าต่างระหว่างทำงาน");
            progress.Value = 0; progress.Animating = true; percentText.Text = "0%";
            phaseText.Text = uninstall ? "กำลังคืนไฟล์เดิม..." : "กำลังเตรียมติดตั้ง..."; SetControls(false);
            string progressDirectory = Path.Combine(Path.GetTempPath(), "DRV3ThaiMod");
            Directory.CreateDirectory(progressDirectory);
            progressFile = Path.Combine(progressDirectory, Guid.NewGuid().ToString("N") + ".log");
            progressCharacters = 0;
            var arguments = (uninstall ? "--uninstall " : "") + "--no-pause --game \"" + game + "\" --progress-file \"" + progressFile + "\"";
            var info = new ProcessStartInfo(backend, arguments);
            info.UseShellExecute = true; info.Verb = "runas"; info.WindowStyle = ProcessWindowStyle.Hidden;
            worker = new Process { StartInfo = info, EnableRaisingEvents = true };
            worker.Exited += delegate { BeginInvoke(new Action(FinishWorker)); };
            try
            {
                worker.Start();
                progressTimer = new System.Windows.Forms.Timer { Interval = 200 };
                progressTimer.Tick += delegate { ReadProgressFile(); };
                progressTimer.Start();
            }
            catch (Exception ex)
            {
                worker = null;
                progress.Animating = false;
                SetControls(true);
                string message = ex is System.ComponentModel.Win32Exception && ((System.ComponentModel.Win32Exception)ex).NativeErrorCode == 1223
                    ? "ผู้ใช้ยกเลิกการอนุญาต Administrator จึงยังไม่ได้ติดตั้ง"
                    : ex.Message;
                DrDialog.Error(this, "เปิดตัวติดตั้งไม่ได้", message);
            }
        }

        void ReadProgressFile()
        {
            if (String.IsNullOrEmpty(progressFile) || !File.Exists(progressFile)) return;
            try
            {
                string content;
                using (var stream = new FileStream(progressFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream, Encoding.UTF8, true)) content = reader.ReadToEnd();
                if (content.Length <= progressCharacters) return;
                string added = content.Substring(progressCharacters);
                progressCharacters = content.Length;
                string[] lines = added.Replace("\r", "").Split('\n');
                foreach (string line in lines) if (!String.IsNullOrWhiteSpace(line)) HandleLine(line);
            }
            catch (IOException) { }
        }

        void HandleLine(string line)
        {
            AppendLog(line);
            Match m = Regex.Match(line, @"(\d+)\s*/\s*(\d+)");
            if (m.Success)
            {
                int done = Int32.Parse(m.Groups[1].Value), total = Int32.Parse(m.Groups[2].Value);
                int value = Math.Min(99, done * 100 / total); progress.Value = value; percentText.Text = value + "%";
                phaseText.Text = String.Format("กำลังแตกไฟล์เกม — {0:N0}/{1:N0}", done, total);
            }
            else if (line.Contains("ตรวจสอบ")) phaseText.Text = "กำลังตรวจสอบไฟล์ Steam...";
            else if (line.Contains("ใส่ไฟล์ภาษาไทย")) { progress.Value = 99; percentText.Text = "99%"; phaseText.Text = "กำลังใส่คำแปลภาษาไทย..."; }
        }

        void FinishWorker()
        {
            if (progressTimer != null) { progressTimer.Stop(); progressTimer.Dispose(); progressTimer = null; }
            ReadProgressFile();
            int code = worker.ExitCode;
            progress.Animating = false;
            progress.Value = code == 0 ? 100 : progress.Value;
            percentText.Text = code == 0 ? "100%" : percentText.Text;
            percentText.ForeColor = code == 0 ? Theme.Green : Theme.Red;
            SetControls(true); RefreshState();
            if (code == 0) { phaseText.Text = "ดำเนินการสำเร็จ"; DrDialog.Info(this, "สำเร็จ", "ดำเนินการเรียบร้อยแล้ว\r\n\r\n\r\nเปิดเกมจาก Steam ได้เลย"); }
            else { phaseText.Text = "การทำงานไม่สำเร็จ"; DrDialog.Error(this, "ไม่สำเร็จ", "เกิดข้อผิดพลาด กรุณาดูรายละเอียดในช่องแสดงผลด้านล่าง"); }
            try { if (!String.IsNullOrEmpty(progressFile) && File.Exists(progressFile)) File.Delete(progressFile); } catch (IOException) { }
        }

        void SetControls(bool enabled) { gamePath.Enabled = browseButton.Enabled = enabled; installButton.Enabled = uninstallButton.Enabled = enabled; if (enabled) RefreshState(); }

        void ShowLogPlaceholder()
        {
            log.Clear();
            log.SelectionColor = Theme.Dim;
            log.AppendText("  รายละเอียดการทำงานจะแสดงที่นี่" + Environment.NewLine);
            log.AppendText("  ตรวจตำแหน่งเกมด้านบน แล้วกดปุ่มด้านล่างเพื่อเริ่ม" + Environment.NewLine);
            log.SelectionColor = log.ForeColor;
        }

        void AppendLog(string line)
        {
            Color color = Color.FromArgb(198, 196, 212);
            if (line.IndexOf("ผิดพลาด", StringComparison.Ordinal) >= 0 || line.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)
                color = Theme.Red;
            else if (line.IndexOf("สำเร็จ", StringComparison.Ordinal) >= 0 || line.IndexOf("verified", StringComparison.OrdinalIgnoreCase) >= 0)
                color = Theme.Green;
            else if (line.IndexOf("คำเตือน", StringComparison.Ordinal) >= 0)
                color = Theme.Yellow;
            else if (Regex.IsMatch(line, @"\d+\s*/\s*\d+"))
                color = Theme.Cyan;

            log.SelectionStart = log.TextLength;
            log.SelectionLength = 0;
            log.SelectionColor = color;
            log.AppendText(line + Environment.NewLine);
            log.SelectionColor = log.ForeColor;
            log.SelectionStart = log.TextLength;
            log.ScrollToCaret();
        }

        void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (worker != null && !worker.HasExited) { e.Cancel = true; DrDialog.Warn(this, "กำลังทำงาน", "กรุณารอให้การทำงานเสร็จก่อนปิดหน้าต่าง"); }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new InstallerForm()); }
    }
}
