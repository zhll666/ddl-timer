using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ChronoRuler
{
    internal static class Ui
    {
        public static float Scale = 1f;

        public static int D(float v)
        {
            return (int)Math.Round(v * Scale);
        }

        public static float DF(float v)
        {
            return v * Scale;
        }

        public static Rectangle R(float x, float y, float w, float h)
        {
            return new Rectangle(D(x), D(y), Math.Max(0, D(w)), Math.Max(0, D(h)));
        }

        public static RectangleF RF(float x, float y, float w, float h)
        {
            return new RectangleF(DF(x), DF(y), DF(w), DF(h));
        }

        public static Color Hex(string hex)
        {
            hex = hex.Replace("#", "");
            int v = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return Color.FromArgb(255, (v >> 16) & 255, (v >> 8) & 255, v & 255);
        }

        public static Color Alpha(Color c, int a)
        {
            if (a < 0) a = 0;
            if (a > 255) a = 255;
            return Color.FromArgb(a, c);
        }

        public static Color Mix(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static Color Lighten(Color c, float t)
        {
            return Mix(c, Color.White, t);
        }

        public static Color Darken(Color c, float t)
        {
            return Mix(c, Color.Black, t);
        }

        public static string FmtDur(long ms)
        {
            if (ms < 0) ms = 0;
            long mins = ms / 60000;
            long h = mins / 60;
            long m = mins % 60;
            if (h > 0) return m == 0 ? h + "h" : h + "h " + m + "m";
            return mins + "m";
        }

        public static string FmtRemain(long ms)
        {
            if (ms < 0) ms = 0;
            long total = ms / 1000;
            long d = total / 86400;
            long h = (total % 86400) / 3600;
            long m = (total % 3600) / 60;
            long s = total % 60;
            if (d > 0) return h > 0 ? d + "天 " + h + "h" : d + "天";
            if (h > 0) return h + "h " + m.ToString("00") + "m";
            if (m > 0) return m + "m " + s.ToString("00") + "s";
            return s + "s";
        }

        public static string FmtRemainHM(long ms)
        {
            if (ms <= 0) return "00:00";
            long mins = (ms + 59999) / 60000;
            long h = mins / 60;
            long m = mins % 60;
            return h.ToString("00") + ":" + m.ToString("00");
        }

        public static string FmtTimer(long ms)
        {
            if (ms < 0) ms = 0;
            long total = ms / 1000;
            long h = total / 3600;
            long m = (total % 3600) / 60;
            long s = total % 60;
            if (h > 0) return h.ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00");
            return m.ToString("00") + ":" + s.ToString("00");
        }

        public static string FmtClock(DateTime t)
        {
            return t.ToString("HH:mm:ss");
        }

        public static string FmtHM(DateTime t)
        {
            return t.ToString("HH:mm");
        }

        public static string FmtHM(long epochMs)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(epochMs).ToLocalTime().ToString("HH:mm");
        }

        public static string FmtDate(DateTime d)
        {
            return d.ToString("yyyy年M月d日");
        }

        public static string Weekday(DateTime d)
        {
            string[] names = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            return names[(int)d.DayOfWeek];
        }

        public static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static long ToMs(DateTime local)
        {
            return new DateTimeOffset(local).ToUnixTimeMilliseconds();
        }

        public static bool SameDay(DateTime a, DateTime b)
        {
            return a.Year == b.Year && a.Month == b.Month && a.Day == b.Day;
        }

        public static string Plural(long n, string one, string many)
        {
            return n + " " + (n == 1 ? one : many);
        }
    }

    internal static class Pal
    {
        public static readonly Color Bg = Ui.Hex("#0A0C12");
        public static readonly Color BgTop = Ui.Hex("#0D1018");
        public static readonly Color Panel = Ui.Hex("#111620");
        public static readonly Color PanelAlt = Ui.Hex("#151B27");
        public static readonly Color PanelHi = Ui.Hex("#1B2231");
        public static readonly Color Field = Ui.Hex("#0C1017");
        public static readonly Color Border = Ui.Hex("#232B3A");
        public static readonly Color BorderSoft = Ui.Hex("#1A202C");
        public static readonly Color Text = Ui.Hex("#F0F3F9");
        public static readonly Color TextSoft = Ui.Hex("#C6CDDA");
        public static readonly Color Dim = Ui.Hex("#8B94A7");
        public static readonly Color Muted = Ui.Hex("#626C7F");
        public static readonly Color Accent = Ui.Hex("#7C6BFF");
        public static readonly Color AccentLight = Ui.Hex("#A596FF");
        public static readonly Color Coral = Ui.Hex("#FF6B81");
        public static readonly Color Green = Ui.Hex("#5BE0B5");
        public static readonly Color Amber = Ui.Hex("#FFC46B");
        public static readonly Color Danger = Ui.Hex("#F2555A");
        public static readonly Color White = Color.White;

        public static readonly Color[] Projects =
        {
            Ui.Hex("#8B8BFF"),
            Ui.Hex("#5B9DFF"),
            Ui.Hex("#5BE0B5"),
            Ui.Hex("#FFC46B"),
            Ui.Hex("#FF8FB1"),
            Ui.Hex("#4FD1C5"),
            Ui.Hex("#FF7A6B"),
            Ui.Hex("#C084FC"),
            Ui.Hex("#C6E86B"),
            Ui.Hex("#53C8E8")
        };

        public static Color ProjectColor(int i)
        {
            if (i < 0) i = 0;
            return Projects[i % Projects.Length];
        }
    }

    internal static class Fonts
    {
        public static readonly Font Clock = new Font("Segoe UI", 34f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font ClockSec = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font NumXL = new Font("Segoe UI Semibold", 20f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font NumL = new Font("Segoe UI Semibold", 14f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font NumRemain = new Font("Segoe UI Semibold", 16f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font NumM = new Font("Segoe UI Semibold", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font NumS = new Font("Segoe UI Semibold", 9f, FontStyle.Regular, GraphicsUnit.Point);

        public static readonly Font H1 = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font H2 = new Font("Microsoft YaHei UI", 11.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font H3 = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Body = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font BodyBold = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Small = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font SmallBold = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Tiny = new Font("Microsoft YaHei UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font TinyBold = new Font("Microsoft YaHei UI", 8f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Input = new Font("Microsoft YaHei UI", 12f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font TimeInput = new Font("Segoe UI Semibold", 16f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Logo = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold, GraphicsUnit.Point);
    }

    internal static class Gfx
    {
        private static readonly Dictionary<int, StringFormat> FmtCache = new Dictionary<int, StringFormat>();

        private static StringFormat Fmt(StringAlignment h, StringAlignment v, bool trim)
        {
            int key = ((int)h * 3 + (int)v) * 2 + (trim ? 1 : 0);
            StringFormat sf;
            if (!FmtCache.TryGetValue(key, out sf))
            {
                sf = new StringFormat();
                sf.Alignment = h;
                sf.LineAlignment = v;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                if (trim) sf.Trimming = StringTrimming.EllipsisCharacter;
                FmtCache[key] = sf;
            }
            return sf;
        }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0)
            {
                p.AddRectangle(r);
                return p;
            }
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            if (d <= 0.5f)
            {
                p.AddRectangle(r);
                return p;
            }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, RectangleF r, float radius, Color c)
        {
            using (GraphicsPath p = Round(r, radius))
            using (SolidBrush b = new SolidBrush(c))
                g.FillPath(b, p);
        }

        public static void FillRound(Graphics g, RectangleF r, float radius, Brush b)
        {
            using (GraphicsPath p = Round(r, radius))
                g.FillPath(b, p);
        }

        public static void StrokeRound(Graphics g, RectangleF r, float radius, Color c, float width)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Pen pen = new Pen(c, width))
                g.DrawPath(pen, p);
        }

        public static void Text(Graphics g, string s, Font f, Color c, RectangleF r,
            StringAlignment hAlign, StringAlignment vAlign, bool trim)
        {
            if (s == null) s = "";
            using (SolidBrush b = new SolidBrush(c))
                g.DrawString(s, f, b, r, Fmt(hAlign, vAlign, trim));
        }

        public static void TextL(Graphics g, string s, Font f, Color c, RectangleF r)
        {
            Text(g, s, f, c, r, StringAlignment.Near, StringAlignment.Center, true);
        }

        public static void TextR(Graphics g, string s, Font f, Color c, RectangleF r)
        {
            Text(g, s, f, c, r, StringAlignment.Far, StringAlignment.Center, true);
        }

        public static void TextC(Graphics g, string s, Font f, Color c, RectangleF r)
        {
            Text(g, s, f, c, r, StringAlignment.Center, StringAlignment.Center, true);
        }

        public static void TextLT(Graphics g, string s, Font f, Color c, RectangleF r)
        {
            Text(g, s, f, c, r, StringAlignment.Near, StringAlignment.Near, true);
        }

        public static void TextRaw(Graphics g, string s, Font f, Color c, float x, float y)
        {
            using (SolidBrush b = new SolidBrush(c))
                g.DrawString(s, f, b, x, y, StringFormat.GenericTypographic);
        }

        public static SizeF Measure(Graphics g, string s, Font f)
        {
            if (s == null) s = "";
            return g.MeasureString(s, f, 100000, StringFormat.GenericTypographic);
        }

        public static void VGradient(Graphics g, RectangleF r, Color top, Color bottom)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new PointF(r.X, r.Y), new PointF(r.X, r.Bottom), top, bottom))
                g.FillRectangle(b, r);
        }

        public static void HGradient(Graphics g, RectangleF r, Color left, Color right)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new PointF(r.X, r.Y), new PointF(r.Right, r.Y), left, right))
                g.FillRectangle(b, r);
        }

        public static void Glow(Graphics g, RectangleF r, Color c, int alpha)
        {
            if (r.Width <= 0 || r.Height <= 0 || alpha <= 0) return;
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(r);
                using (PathGradientBrush b = new PathGradientBrush(p))
                {
                    b.CenterColor = Ui.Alpha(c, alpha);
                    b.SurroundColors = new Color[] { Ui.Alpha(c, 0) };
                    b.CenterPoint = new PointF(r.X + r.Width / 2f, r.Y + r.Height / 2f);
                    g.FillPath(b, p);
                }
            }
        }

        public static void HLine(Graphics g, float x1, float x2, float y, Color c, float width)
        {
            using (Pen p = new Pen(c, width))
                g.DrawLine(p, x1, y, x2, y);
        }

        public static void VLine(Graphics g, float x, float y1, float y2, Color c, float width)
        {
            using (Pen p = new Pen(c, width))
                g.DrawLine(p, x, y1, x, y2);
        }

        public static void Dot(Graphics g, float cx, float cy, float radius, Color c)
        {
            using (SolidBrush b = new SolidBrush(c))
                g.FillEllipse(b, cx - radius, cy - radius, radius * 2f, radius * 2f);
        }

        public static void Chevron(Graphics g, RectangleF r, int dir, Color c, float width)
        {
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            float s = Math.Min(r.Width, r.Height) * 0.22f;
            PointF a, b, d;
            if (dir < 0)
            {
                a = new PointF(cx + s * 0.6f, cy - s);
                b = new PointF(cx - s * 0.6f, cy);
                d = new PointF(cx + s * 0.6f, cy + s);
            }
            else
            {
                a = new PointF(cx - s * 0.6f, cy - s);
                b = new PointF(cx + s * 0.6f, cy);
                d = new PointF(cx - s * 0.6f, cy + s);
            }
            using (Pen p = new Pen(c, width))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p, new PointF[] { a, b, d });
            }
        }

        public static void Play(Graphics g, RectangleF r, Color c)
        {
            float w = r.Width, h = r.Height;
            PointF[] pts =
            {
                new PointF(r.X + w * 0.32f, r.Y + h * 0.22f),
                new PointF(r.X + w * 0.78f, r.Y + h * 0.5f),
                new PointF(r.X + w * 0.32f, r.Y + h * 0.78f)
            };
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddPolygon(pts);
                using (SolidBrush b = new SolidBrush(c))
                    g.FillPath(b, p);
            }
        }

        public static void Stop(Graphics g, RectangleF r, Color c)
        {
            float w = Math.Min(r.Width, r.Height) * 0.34f;
            float x = r.X + (r.Width - w) / 2f;
            float y = r.Y + (r.Height - w) / 2f;
            FillRound(g, new RectangleF(x, y, w, w), w * 0.25f, c);
        }

        public static void Plus(Graphics g, RectangleF r, Color c, float width)
        {
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            float s = Math.Min(r.Width, r.Height) * 0.26f;
            using (Pen p = new Pen(c, width))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawLine(p, cx - s, cy, cx + s, cy);
                g.DrawLine(p, cx, cy - s, cx, cy + s);
            }
        }

        public static void Minus(Graphics g, RectangleF r, Color c, float width)
        {
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            float s = Math.Min(r.Width, r.Height) * 0.26f;
            using (Pen p = new Pen(c, width))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawLine(p, cx - s, cy, cx + s, cy);
            }
        }

        public static void CloseX(Graphics g, RectangleF r, Color c, float width)
        {
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            float s = Math.Min(r.Width, r.Height) * 0.23f;
            using (Pen p = new Pen(c, width))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawLine(p, cx - s, cy - s, cx + s, cy + s);
                g.DrawLine(p, cx + s, cy - s, cx - s, cy + s);
            }
        }

        public static void Square(Graphics g, RectangleF r, Color c, float width, bool filled)
        {
            RectangleF q = new RectangleF(
                r.X + r.Width / 2f - r.Width * 0.18f,
                r.Y + r.Height / 2f - r.Height * 0.18f,
                r.Width * 0.36f, r.Height * 0.36f);
            if (filled)
            {
                FillRound(g, q, 1f, c);
            }
            else
            {
                using (Pen p = new Pen(c, width))
                    g.DrawRectangle(p, q.X, q.Y, q.Width, q.Height);
            }
        }

        public static void Restore(Graphics g, RectangleF r, Color c, float width)
        {
            float s = Math.Min(r.Width, r.Height) * 0.30f;
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            using (Pen p = new Pen(c, width))
            {
                g.DrawRectangle(p, cx - s * 0.9f, cy - s * 0.55f, s * 1.5f, s * 1.15f);
                g.DrawLine(p, cx - s * 0.25f, cy - s * 0.55f, cx - s * 0.25f, cy - s * 0.9f);
                g.DrawLine(p, cx + s * 0.6f, cy - s * 0.9f, cx + s * 0.6f, cy + s * 0.6f);
                g.DrawLine(p, cx + s * 0.6f, cy + s * 0.6f, cx - s * 0.9f, cy + s * 0.6f);
            }
        }

        public static void ClockMark(Graphics g, RectangleF r, Color ringA, Color ringB, Color hands, Color dot, bool showTicks)
        {
            float d = Math.Min(r.Width, r.Height);
            RectangleF c = new RectangleF(r.X + (r.Width - d) / 2f, r.Y + (r.Height - d) / 2f, d, d);
            using (Pen bg = new Pen(Ui.Alpha(ringA, 40), Math.Max(1f, d * 0.10f)))
                g.DrawEllipse(bg, c.X + d * 0.06f, c.Y + d * 0.06f, d * 0.88f, d * 0.88f);
            using (LinearGradientBrush lb = new LinearGradientBrush(c, ringA, ringB, 45f))
            using (Pen p = new Pen(lb, Math.Max(1.4f, d * 0.075f)))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawArc(p, c.X + d * 0.06f, c.Y + d * 0.06f, d * 0.88f, d * 0.88f, -55f, 285f);
            }
            if (showTicks)
            {
                using (Pen tp = new Pen(Ui.Alpha(hands, 70), Math.Max(1f, d * 0.025f)))
                {
                    for (int i = 0; i < 12; i++)
                    {
                        double a = i * Math.PI / 6.0 - Math.PI / 2.0;
                        float r1 = d * 0.33f;
                        float r2 = i % 3 == 0 ? d * 0.40f : d * 0.37f;
                        float cx = c.X + d / 2f, cy = c.Y + d / 2f;
                        g.DrawLine(tp,
                            (float)(cx + Math.Cos(a) * r1), (float)(cy + Math.Sin(a) * r1),
                            (float)(cx + Math.Cos(a) * r2), (float)(cy + Math.Sin(a) * r2));
                    }
                }
            }
            float ccx = c.X + d / 2f;
            float ccy = c.Y + d / 2f;
            using (Pen hp = new Pen(hands, Math.Max(1.4f, d * 0.07f)))
            {
                hp.StartCap = LineCap.Round;
                hp.EndCap = LineCap.Round;
                g.DrawLine(hp, ccx, ccy, ccx, ccy - d * 0.26f);
                g.DrawLine(hp, ccx, ccy, ccx + d * 0.20f, ccy + d * 0.14f);
            }
            Dot(g, ccx, ccy - d * 0.26f, Math.Max(1.5f, d * 0.045f), dot);
            Dot(g, ccx, ccy, Math.Max(1.5f, d * 0.035f), hands);
        }
    }

    internal static class Chrome
    {
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public static void Apply(IntPtr handle)
        {
            try
            {
                int dark = 1;
                DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
                int round = 2;
                DwmSetWindowAttribute(handle, 33, ref round, sizeof(int));
                int border = 0x003A2B23;
                DwmSetWindowAttribute(handle, 34, ref border, sizeof(int));
            }
            catch
            {
            }
        }
    }
}
