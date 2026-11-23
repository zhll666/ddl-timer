using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ChronoRuler
{
    public partial class MainForm
    {
        private Rectangle _rEmptyCard;
        private Rectangle _rEmptyNew;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ComputeLayout();
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            DrawBackground(g);
            DrawTitleBar(g);
            DrawHeader(g);
            DrawProjectRuler(g);
            DrawTimeline(g);
            DrawHorizontalScroll(g);
            DrawFooter(g);
            DrawToast(g);
            DrawWindowBorder(g);
        }

        private void DrawBackground(Graphics g)
        {
            Rectangle r = ClientRectangle;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Point(0, 0), new Point(0, Math.Max(1, r.Height)), Pal.BgTop, Pal.Bg))
                g.FillRectangle(b, r);

            Gfx.Glow(g, new RectangleF(-Ui.D(240), Ui.D(-160), Ui.D(920), Ui.D(620)), Pal.Accent, 22);
            Gfx.Glow(g, new RectangleF(ClientSize.Width - Ui.D(600), Ui.D(-220),
                Ui.D(900), Ui.D(600)), Pal.Coral, 10);
        }

        private bool Hover(Rectangle r)
        {
            return _mouseInside && r.Contains(_mouse);
        }

        private void DrawTitleBar(Graphics g)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(235, 12, 15, 22)))
                g.FillRectangle(b, _rTitle);
            Gfx.HLine(g, 0, ClientSize.Width, _rTitle.Bottom - 1, Color.FromArgb(230, 27, 34, 48), 1);

            Gfx.ClockMark(g, _rLogo, Pal.Accent, Pal.Coral, Pal.Text, Pal.Coral, false);
            Gfx.TextLT(g, "ddl计时器", Fonts.Logo, Pal.Text,
                new RectangleF(_rLogo.Right + Ui.D(9), Ui.D(9), Ui.D(120), Ui.D(22)));
            Gfx.TextLT(g, "D D L   T I M E R", Fonts.Tiny,
                Ui.Alpha(Pal.Muted, 210),
                new RectangleF(_rLogo.Right + Ui.D(118), Ui.D(14), Ui.D(160), Ui.D(16)));

            DrawWindowButton(g, _rBtnMin, 0);
            DrawWindowButton(g, _rBtnMax, 1);
            DrawWindowButton(g, _rBtnClose, 2);
        }

        private void DrawWindowButton(Graphics g, Rectangle r, int kind)
        {
            bool hover = Hover(r);
            if (hover)
            {
                Color fill = kind == 2 ? Pal.Danger : Pal.PanelHi;
                using (SolidBrush b = new SolidBrush(fill))
                    g.FillRectangle(b, r);
            }
            Color icon = kind == 2 && hover ? Color.White : Pal.TextSoft;
            Rectangle ir = new Rectangle(r.X + Ui.D(15), r.Y + Ui.D(14), Ui.D(16), Ui.D(16));
            if (kind == 0) Gfx.Minus(g, ir, icon, Ui.DF(1.2f));
            else if (kind == 2) Gfx.CloseX(g, ir, icon, Ui.DF(1.2f));
            else
            {
                if (WindowState == FormWindowState.Maximized)
                    Gfx.Restore(g, ir, icon, Ui.DF(1.1f));
                else
                    Gfx.Square(g, ir, icon, Ui.DF(1.1f), false);
            }
        }

        private void DrawHeader(Graphics g)
        {
            Rectangle h = _rHeader;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Point(0, h.Top), new Point(0, h.Bottom),
                Color.FromArgb(82, 16, 21, 32), Color.FromArgb(0, 12, 15, 22)))
                g.FillRectangle(b, h);

            DrawMinuteReader(g);

            // big live clock
            string hm = _now.ToString("HH:mm");
            SizeF hmSize = Gfx.Measure(g, hm, Fonts.Clock);
            Gfx.TextL(g, hm, Fonts.Clock, Pal.Text,
                new RectangleF(Ui.D(22), h.Top + Ui.D(41), Ui.D(290), Ui.D(50)));
            string secs = ":" + _now.ToString("ss");
            float secX = Ui.D(22) + hmSize.Width + Ui.D(8);
            Gfx.TextL(g, secs, Fonts.ClockSec, Pal.Dim,
                new RectangleF(secX, h.Top + Ui.D(50), Ui.D(82), Ui.D(28)));

            // stable current date; this board is always future-facing
            Gfx.Dot(g, Ui.D(28), h.Top + Ui.D(114), Ui.DF(2.4f), Pal.Green);
            Gfx.TextL(g, "实时 · " + Ui.FmtDate(_now) + "  " + Ui.Weekday(_now),
                Fonts.Body, Pal.TextSoft,
                new RectangleF(Ui.D(37), h.Top + Ui.D(99), Ui.D(330), Ui.D(28)));

            DrawNewProjectButton(g);

            Gfx.HLine(g, 0, ClientSize.Width, _rHeader.Bottom - 1, Color.FromArgb(220, 25, 31, 43), 1);
        }

        private void DrawMinuteReader(Graphics g)
        {
            RectangleF bar = _rMinuteBar;
            Rectangle h = _rHeader;

            Gfx.Dot(g, Ui.D(28), h.Top + Ui.D(24), Ui.DF(2.5f), Pal.Coral);
            Gfx.TextL(g, "一分钟读秒", Fonts.SmallBold, Pal.Dim,
                new RectangleF(Ui.D(37), h.Top + Ui.D(12), Ui.D(92), Ui.D(22)));
            string secText = _now.Second.ToString("00");
            SizeF secSize = Gfx.Measure(g, secText, Fonts.NumXL);
            Gfx.TextRaw(g, secText, Fonts.NumXL, Pal.Text,
                Ui.DF(102), h.Top + Ui.D(1));
            Gfx.TextLT(g, "″", Fonts.Tiny, Pal.Dim,
                new RectangleF(Ui.D(104) + secSize.Width, h.Top + Ui.D(16), Ui.D(14), Ui.D(14)));

            Gfx.FillRound(g, bar, Ui.DF(3f), Color.FromArgb(255, 8, 11, 17));
            RectangleF inner = new RectangleF(bar.X + 2, bar.Y + 2,
                Math.Max(0, bar.Width - 4), Math.Max(0, bar.Height - 4));
            double frac = (_now.Second + _now.Millisecond / 1000.0) / 60.0;
            float fillW = (float)(inner.Width * frac);
            if (fillW > 0.5f)
            {
                GraphicsState fillState = g.Save();
                g.SetClip(inner);
                using (LinearGradientBrush b = new LinearGradientBrush(
                    new PointF(inner.X, inner.Y), new PointF(inner.Right, inner.Y),
                    Color.FromArgb(225, Pal.Coral), Color.FromArgb(210, Pal.Amber)))
                    g.FillRectangle(b, inner.X, inner.Y, fillW, inner.Height);
                Gfx.HLine(g, inner.X, inner.X + fillW, inner.Bottom - 1,
                    Color.FromArgb(120, 255, 255, 255), 1f);
                g.Restore(fillState);
            }

            for (int i = 1; i < 60; i++)
            {
                float x = inner.X + inner.Width * i / 60f;
                bool quarter = i % 15 == 0;
                bool five = i % 5 == 0;
                float hh = quarter ? 11f : (five ? 8f : 5f);
                Color tc = i <= _now.Second
                    ? Color.FromArgb(72, 255, 255, 255)
                    : Color.FromArgb(quarter ? 85 : 42, 120, 132, 152);
                using (Pen p = new Pen(tc, 1f))
                    g.DrawLine(p, x, inner.Bottom - hh, x, inner.Bottom - 1);
            }

            Gfx.StrokeRound(g, bar, Ui.DF(3f), Color.FromArgb(210, 55, 66, 88), 1f);
            if (fillW > 1f)
            {
                float edge = inner.X + fillW;
                using (Pen p = new Pen(Color.FromArgb(245, 255, 235, 205), Ui.DF(2f)))
                    g.DrawLine(p, edge, inner.Y, edge, inner.Bottom);
                Gfx.Glow(g, new RectangleF(edge - 10, bar.Y - 4, 20, bar.Height + 8), Pal.Coral, 90);
            }

        }

        private void DrawNavButton(Graphics g, Rectangle r, int dir)
        {
            if (Hover(r))
                Gfx.FillRound(g, r, Ui.DF(6f), Pal.PanelHi);
            Gfx.Chevron(g, r, dir, Hover(r) ? Pal.Text : Pal.Dim, Ui.DF(1.3f));
        }

        private void DrawNewProjectButton(Graphics g)
        {
            Rectangle r = _rNew;
            bool hover = Hover(r);
            Color top = hover ? Ui.Lighten(Pal.Accent, 0.10f) : Pal.Accent;
            Color bottom = hover ? Ui.Lighten(Pal.Accent, 0.02f) : Ui.Darken(Pal.Accent, 0.12f);
            Gfx.Glow(g, new RectangleF(r.X - 8, r.Y - 4, r.Width + 16, r.Height + 16), Pal.Accent, hover ? 40 : 22);
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Point(r.X, r.Y), new Point(r.X, r.Bottom), top, bottom))
                Gfx.FillRound(g, r, Ui.DF(10f), b);
            Gfx.StrokeRound(g, r, Ui.DF(10f), Ui.Alpha(Color.White, 26), 1f);
            Gfx.Plus(g, new Rectangle(r.X + Ui.D(12), r.Y, Ui.D(22), r.Height), Color.White, Ui.DF(1.5f));
            Gfx.TextL(g, "新建项目", Fonts.BodyBold, Color.White,
                new RectangleF(r.X + Ui.D(34), r.Y, r.Width - Ui.D(40), r.Height));
        }

        private void DrawProjectRuler(Graphics g)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(247, 13, 17, 25)))
                g.FillRectangle(b, _rRuler);
            Gfx.HLine(g, 0, ClientSize.Width, _rRuler.Bottom - 1, Color.FromArgb(220, 27, 34, 47), 1);

            // top-left corner: the top of the vertical time axis
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 10, 13, 20)))
                g.FillRectangle(b, new Rectangle(0, _rRuler.Top, _gutterW, _rRuler.Height));
            Gfx.VLine(g, _gutterW - 1f, _rRuler.Top, _rRuler.Bottom, Color.FromArgb(255, 45, 56, 76), 2);
            Gfx.TextLT(g, "项目序数", Fonts.TinyBold, Pal.Muted,
                new RectangleF(Ui.D(20), _rRuler.Top + Ui.D(8), Ui.D(74), Ui.D(16)));
            Gfx.TextLT(g, "时间 ↓", Fonts.H3, Pal.Text,
                new RectangleF(Ui.D(20), _rRuler.Top + Ui.D(24), Ui.D(84), Ui.D(22)));
            Gfx.TextLT(g, "长度 = 剩余", Fonts.Tiny, Pal.Muted,
                new RectangleF(Ui.D(20), _rRuler.Top + Ui.D(50), Ui.D(84), Ui.D(14)));

            long nowMs = NowMs;
            for (int i = 0; i < _data.Projects.Count; i++)
            {
                Project p = _data.Projects[i];
                Rectangle col = _colRects[i];
                if (col.IsEmpty) continue;
                Rectangle head = new Rectangle(col.X, _rRuler.Top, col.Width, _rRuler.Height);
                Color color = ProjectColor(p);
                bool hover = Hover(head);
                if (hover)
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(18, 255, 255, 255)))
                        g.FillRectangle(b, head);

                Gfx.VLine(g, col.X + 1.5f, _rRuler.Top + 6, _rRuler.Bottom - 6,
                    Ui.Alpha(color, 150), Ui.DF(2f));
                if (i > 0)
                    Gfx.VLine(g, col.X + 0.5f, _rRuler.Top + 14, _rRuler.Bottom,
                        Color.FromArgb(190, 30, 37, 50), 1);

                Gfx.Dot(g, col.X + Ui.D(15), _rRuler.Top + Ui.D(15), Ui.DF(3.2f), color);
                Gfx.TextL(g, p.Ordinal + "-（" + p.Name + "）", Fonts.BodyBold, Pal.Text,
                    new RectangleF(col.X + Ui.D(24), _rRuler.Top + Ui.D(5),
                        Math.Max(20, col.Width - Ui.D(36)), Ui.D(21)));

                long remain = p.EndAt - nowMs;
                string remainText;
                Color remainColor;
                if (p.EndAt <= 0)
                {
                    remainText = "--:--";
                    remainColor = Pal.Dim;
                }
                else if (remain > 0)
                {
                    remainText = Ui.FmtRemainHM(remain);
                    remainColor = Pal.Text;
                }
                else
                {
                    remainText = "00:00";
                    remainColor = Pal.Coral;
                }

                float valueX;
                Font valueFont;
                if (col.Width >= Ui.D(154))
                {
                    Gfx.TextL(g, "剩余", Fonts.Tiny, Pal.Muted,
                        new RectangleF(col.X + Ui.D(24), _rRuler.Top + Ui.D(31), Ui.D(34), Ui.D(17)));
                    valueX = col.X + Ui.D(55);
                    valueFont = Fonts.NumRemain;
                }
                else
                {
                    valueX = col.X + Ui.D(12);
                    valueFont = Fonts.NumS;
                }
                Gfx.TextL(g, remainText, valueFont, remainColor,
                    new RectangleF(valueX, _rRuler.Top + Ui.D(25),
                        Math.Max(10, col.Width - (valueX - col.X) - Ui.D(12)), Ui.D(24)));

                string endText = p.EndAt <= 0
                    ? "未设置截止"
                    : (remain > 0 ? "截止 " + EndLabel(p.EndAt) : "已结束 · " + EndLabel(p.EndAt));
                Gfx.TextL(g, endText, Fonts.Tiny, Ui.Alpha(remain > 0 ? Pal.Dim : Pal.Coral, 220),
                    new RectangleF(col.Width >= Ui.D(154) ? col.X + Ui.D(24) : col.X + Ui.D(12),
                        _rRuler.Top + Ui.D(52),
                        Math.Max(10, col.Width - Ui.D(36)), Ui.D(16)));
            }
        }

        private string EndLabel(long endAt)
        {
            DateTime end = DateTimeOffset.FromUnixTimeMilliseconds(endAt).ToLocalTime().DateTime;
            if (end.Date == DateTime.Today) return Ui.FmtHM(endAt);
            if (end.Date == DateTime.Today.AddDays(1)) return "明天 " + Ui.FmtHM(endAt);
            if (end.Year == DateTime.Today.Year) return end.ToString("M/d HH:mm");
            return end.ToString("yyyy/M/d HH:mm");
        }

        private void DrawTimeline(Graphics g)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 10, 13, 20)))
                g.FillRectangle(b, _rTimeline);

            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(0, _rTimeline.Top, ClientSize.Width, _rTimeline.Height));

            DrawColumnsBackground(g);
            DrawGrid(g);
            DrawBars(g);
            DrawGutter(g);
            DrawNowLine(g);
            DrawEmptyOrHint(g);

            g.Restore(state);
            Gfx.VLine(g, _gutterW - 1f, _rTimeline.Top, _rTimeline.Bottom,
                Color.FromArgb(120, Pal.Accent), Ui.DF(2f));
            Gfx.VLine(g, _gutterW - 0.5f, _rTimeline.Top, _rTimeline.Bottom,
                Color.FromArgb(230, 49, 61, 84), 1);
            Gfx.HLine(g, 0, ClientSize.Width, _rFooter.Top - 1, Color.FromArgb(230, 27, 34, 47), 1);
        }

        private int LabelStepMinutes()
        {
            int[] steps = { 1, 2, 5, 10, 15, 30, 60, 120, 240, 360, 720, 1440 };
            for (int i = 0; i < steps.Length; i++)
            {
                if (_pxh * steps[i] / 60f >= Ui.D(46)) return steps[i];
            }
            return 1440;
        }

        private int TickMinutes()
        {
            if (_pxh >= 300f) return 1;
            if (_pxh >= 130f) return 5;
            if (_pxh >= 45f) return 15;
            if (_pxh >= 20f) return 60;
            if (_pxh >= 8f) return 180;
            if (_pxh >= 4f) return 360;
            return 720;
        }

        private DateTime FirstFutureTick(DateTime top, int step)
        {
            int minuteOfDay = top.Hour * 60 + top.Minute + (top.Second > 0 ? 1 : 0);
            int first = ((minuteOfDay + step - 1) / step) * step;
            DateTime t = top.Date.AddMinutes(first);
            if (t <= top) t = t.AddMinutes(step);
            return t;
        }

        private void DrawGutter(Graphics g)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 9, 12, 19)))
                g.FillRectangle(b, _rGutter);

            DateTime top = TopTime;
            DateTime bottom = top.AddMinutes(_rTimeline.Height / _pxh * 60.0);
            int step = LabelStepMinutes();
            int tickStep = TickMinutes();

            if (tickStep < step)
            {
                DateTime mt = FirstFutureTick(top, tickStep);
                int guard = 0;
                while (mt <= bottom && guard++ < 5000)
                {
                    int minuteOfDay = mt.Hour * 60 + mt.Minute;
                    if (minuteOfDay % step != 0)
                    {
                        float y = YFor(mt);
                        if (y >= _rTimeline.Top + Ui.D(28) && y <= _rTimeline.Bottom)
                        {
                            using (Pen p = new Pen(Color.FromArgb(58, 68, 79, 101), 1f))
                                g.DrawLine(p, _gutterW - Ui.D(4), y, _gutterW - 1, y);
                        }
                    }
                    mt = mt.AddMinutes(tickStep);
                }
            }

            DateTime t = FirstFutureTick(top, step);
            int count = 0;
            while (t <= bottom && count++ < 2000)
            {
                float y = YFor(t);
                bool midnight = t.Hour == 0 && t.Minute == 0;
                if (y >= _rTimeline.Top + Ui.D(22) && y <= _rTimeline.Bottom - Ui.D(6))
                {
                    string label;
                    Font f;
                    Color c;
                    if (midnight)
                    {
                        string date = t.Year == _now.Year ? t.ToString("M/d") : t.ToString("yy/M/d");
                        label = date + " 00:00";
                        f = Fonts.TinyBold;
                        c = Pal.TextSoft;
                    }
                    else
                    {
                        label = t.ToString("HH:mm");
                        f = _pxh < 45f ? Fonts.TinyBold : Fonts.NumS;
                        c = Pal.TextSoft;
                    }
                    Gfx.TextR(g, label, f, c,
                        new RectangleF(Ui.D(4), y - Ui.D(11), _gutterW - Ui.D(18), Ui.D(22)));
                }

                if (y >= _rTimeline.Top && y <= _rTimeline.Bottom)
                {
                    using (Pen p = new Pen(midnight ? Ui.Alpha(Pal.Coral, 170) : Color.FromArgb(125, 68, 79, 101),
                        midnight ? 1.4f : 1.1f))
                        g.DrawLine(p, _gutterW - Ui.D(11), y, _gutterW - 1, y);
                }
                t = t.AddMinutes(step);
            }

            // The chart origin is "now"; it stays at the top while following.
            if (_scrollY < 1)
            {
                Gfx.Dot(g, Ui.D(11), _rTimeline.Top + Ui.D(11), Ui.DF(2.5f), Pal.Coral);
                Gfx.TextL(g, "现在 " + _now.ToString("HH:mm"), Fonts.TinyBold, Pal.Coral,
                    new RectangleF(Ui.D(18), _rTimeline.Top + Ui.D(3), Ui.D(70), Ui.D(18)));
            }

            using (LinearGradientBrush b = new LinearGradientBrush(
                new Point(0, _rTimeline.Bottom - Ui.D(34)), new Point(0, _rTimeline.Bottom),
                Color.FromArgb(0, 10, 13, 19), Color.FromArgb(120, 10, 13, 19)))
                g.FillRectangle(b, 0, _rTimeline.Bottom - Ui.D(34), _gutterW, Ui.D(34));
        }

        private void DrawColumnsBackground(Graphics g)
        {
            Rectangle clip = new Rectangle(_colX0, _rTimeline.Top, _colRight - _colX0, _rTimeline.Height);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 10, 13, 20)))
                g.FillRectangle(b, clip);

            for (int i = 0; i < _data.Projects.Count; i++)
            {
                Rectangle col = _colRects[i];
                if (col.IsEmpty) continue;
                Color color = ProjectColor(_data.Projects[i]);
                using (SolidBrush b = new SolidBrush(Ui.Alpha(color, 6)))
                    g.FillRectangle(b, col);
                if (i % 2 == 1)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(5, 255, 255, 255)))
                        g.FillRectangle(b, col);
                }
                Gfx.VLine(g, col.X + 1.5f, _rTimeline.Top, _rTimeline.Bottom,
                    Ui.Alpha(color, 30), Ui.DF(1.5f));
                if (i > 0)
                    Gfx.VLine(g, col.X + 0.5f, _rTimeline.Top, _rTimeline.Bottom,
                        Color.FromArgb(255, 27, 34, 47), 1);
            }
        }

        private void DrawGrid(Graphics g)
        {
            if (_data.Projects.Count == 0) return;
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(_colX0, _rTimeline.Top, _colRight - _colX0, _rTimeline.Height));

            DateTime top = TopTime;
            DateTime bottom = top.AddMinutes(_rTimeline.Height / _pxh * 60.0);
            int step = LabelStepMinutes();
            int tickStep = TickMinutes();

            if (tickStep < step)
            {
                DateTime mt = FirstFutureTick(top, tickStep);
                int guard = 0;
                while (mt <= bottom && guard++ < 6000)
                {
                    int minuteOfDay = mt.Hour * 60 + mt.Minute;
                    if (minuteOfDay % step != 0)
                    {
                        float y = YFor(mt);
                        if (y >= _rTimeline.Top && y <= _rTimeline.Bottom)
                            Gfx.HLine(g, _colX0, _colRight, y, Color.FromArgb(13, 52, 62, 82), 1f);
                    }
                    mt = mt.AddMinutes(tickStep);
                }
            }

            DateTime t = FirstFutureTick(top, step);
            int count = 0;
            while (t <= bottom && count++ < 2500)
            {
                float y = YFor(t);
                bool midnight = t.Hour == 0 && t.Minute == 0;
                if (y >= _rTimeline.Top - 1 && y <= _rTimeline.Bottom + 1)
                {
                    Color c = midnight
                        ? Color.FromArgb(125, 82, 96, 124)
                        : Color.FromArgb(48, 52, 63, 84);
                    Gfx.HLine(g, _colX0, _colRight, y, c, midnight ? 1.4f : 1f);
                }
                t = t.AddMinutes(step);
            }

            g.Restore(state);
        }

        private void DrawBars(Graphics g)
        {
            if (_data.Projects.Count == 0) return;
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(_colX0, _rTimeline.Top, _colRight - _colX0, _rTimeline.Height));

            long nowMs = NowMs;
            float originY = _rTimeline.Top - (float)_scrollY;

            for (int i = 0; i < _data.Projects.Count; i++)
            {
                Project p = _data.Projects[i];
                Rectangle col = _colRects[i];
                if (col.IsEmpty) continue;
                Color color = ProjectColor(p);
                long remain = p.EndAt - nowMs;
                float x = col.X + Ui.D(18);
                float w = Math.Max(12, col.Width - Ui.D(36));

                if (remain <= 0)
                {
                    if (originY >= _rTimeline.Top - Ui.D(20) && originY <= _rTimeline.Bottom)
                    {
                        Gfx.TextL(g, "已结束", Fonts.TinyBold, Ui.Alpha(Pal.Coral, 210),
                            new RectangleF(x, originY + Ui.D(8), w, Ui.D(16)));
                    }
                    continue;
                }

                float h = (float)(remain / 3600000.0 * _pxh);
                float y1 = originY;
                float y2 = y1 + h;
                if (y2 < _rTimeline.Top || y1 > _rTimeline.Bottom) continue;

                float drawTop = Math.Max(y1, _rTimeline.Top - 1);
                float drawBottom = Math.Min(y2, _rTimeline.Bottom + 1);
                float dh = Math.Max(2f, drawBottom - drawTop);
                RectangleF r = new RectangleF(x, drawTop, w, dh);
                float radius = y1 >= _rTimeline.Top - 1
                    ? Math.Min(Ui.DF(8f), dh / 2f)
                    : Ui.DF(0.5f);

                using (LinearGradientBrush fill = new LinearGradientBrush(
                    new PointF(r.X, r.Y), new PointF(r.X, r.Bottom),
                    Ui.Alpha(color, 84),
                    Ui.Alpha(color, 18)))
                {
                    Gfx.FillRound(g, r, radius, fill);
                }
                Gfx.StrokeRound(g, r, radius, Ui.Alpha(color, 160), 1.2f);

                // The top edge is aligned with the now line.
                if (y1 >= _rTimeline.Top - 1 && y1 <= _rTimeline.Bottom)
                {
                    Gfx.HLine(g, x + Ui.D(4), x + w - Ui.D(4), y1 + 1,
                        Ui.Alpha(Ui.Lighten(color, 0.4f), 240), Ui.DF(2f));
                }

                // The bottom edge is aligned with the end time.
                if (y2 <= _rTimeline.Bottom && y2 >= _rTimeline.Top)
                {
                    Gfx.HLine(g, x + Ui.D(2), x + w - Ui.D(2), y2 - 1,
                        Ui.Alpha(Ui.Lighten(color, 0.5f), 245), Ui.DF(2f));
                    if (y2 - y1 > Ui.D(26) && w > Ui.D(72))
                    {
                        Gfx.TextR(g, Ui.FmtHM(p.EndAt), Fonts.Tiny, Ui.Alpha(Pal.Text, 220),
                            new RectangleF(x + Ui.D(6), y2 - Ui.D(18), w - Ui.D(12), Ui.D(15)));
                    }
                }

            }

            g.Restore(state);
        }

        private void DrawNowLine(Graphics g)
        {
            if (_scrollY > 0.5) return;
            float y = _rTimeline.Top + 0.5f;

            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(0, _rTimeline.Top, ClientSize.Width, _rTimeline.Height));
            using (Pen glow = new Pen(Ui.Alpha(Pal.Coral, 32), Ui.DF(6f)))
                g.DrawLine(glow, _gutterW, y, _colRight, y);
            using (Pen glow = new Pen(Ui.Alpha(Pal.Coral, 78), Ui.DF(2.6f)))
                g.DrawLine(glow, _gutterW, y, _colRight, y);
            using (Pen line = new Pen(Pal.Coral, Ui.DF(1.3f)))
                g.DrawLine(line, _gutterW, y, _colRight, y);
            Gfx.Dot(g, _gutterW, y, Ui.DF(2.6f), Pal.Coral);
            g.Restore(state);
        }

        private void DrawEmptyOrHint(Graphics g)
        {
            if (_data.Projects.Count == 0)
            {
                Rectangle card = _rEmptyCard;
                Gfx.Glow(g, new RectangleF(card.X - 40, card.Y - 20, card.Width + 80, card.Height + 80),
                    Pal.Accent, 34);
                Gfx.FillRound(g, card, Ui.DF(18f), Color.FromArgb(248, 17, 22, 33));
                Gfx.StrokeRound(g, card, Ui.DF(18f), Ui.Alpha(Pal.Border, 240), 1f);

                RectangleF iconR = new RectangleF(card.X + Ui.D(32), card.Y + Ui.D(30), Ui.D(46), Ui.D(46));
                Gfx.FillRound(g, iconR, Ui.DF(14f), Ui.Alpha(Pal.Accent, 20));
                Gfx.StrokeRound(g, iconR, Ui.DF(14f), Ui.Alpha(Pal.Accent, 80), 1f);
                Gfx.ClockMark(g, new RectangleF(iconR.X + Ui.D(9), iconR.Y + Ui.D(9), Ui.D(28), Ui.D(28)),
                    Pal.Accent, Pal.Coral, Pal.Text, Pal.Coral, false);

                Gfx.TextLT(g, "创建第一个项目", Fonts.H1, Pal.Text,
                    new RectangleF(card.X + Ui.D(32), card.Y + Ui.D(90), card.Width - Ui.D(64), Ui.D(30)));
                Gfx.TextLT(g, "用纵向长条，看见每个目标还剩多少时间。", Fonts.Body, Pal.Dim,
                    new RectangleF(card.X + Ui.D(32), card.Y + Ui.D(123), card.Width - Ui.D(64), Ui.D(24)));
                Gfx.TextLT(g, "顶部始终是现在；拖动图表向下浏览更远的未来。", Fonts.Small, Pal.Muted,
                    new RectangleF(card.X + Ui.D(32), card.Y + Ui.D(146), card.Width - Ui.D(64), Ui.D(20)));

                Rectangle btn = _rEmptyNew;
                bool hover = Hover(btn);
                using (LinearGradientBrush b = new LinearGradientBrush(
                    new Point(btn.X, btn.Y), new Point(btn.X, btn.Bottom),
                    hover ? Ui.Lighten(Pal.Accent, 0.12f) : Pal.Accent,
                    hover ? Pal.Accent : Ui.Darken(Pal.Accent, 0.12f)))
                    Gfx.FillRound(g, btn, Ui.DF(9f), b);
                Gfx.Plus(g, new Rectangle(btn.X + Ui.D(12), btn.Y, Ui.D(20), btn.Height), Color.White, Ui.DF(1.4f));
                Gfx.TextL(g, "新建项目", Fonts.BodyBold, Color.White,
                    new RectangleF(btn.X + Ui.D(32), btn.Y, btn.Width - Ui.D(38), btn.Height));

                Gfx.TextLT(g, "数据仅保存在本机 · 无需登录", Fonts.Tiny, Pal.Muted,
                    new RectangleF(card.X + Ui.D(32), card.Bottom - Ui.D(30), card.Width - Ui.D(64), Ui.D(16)));
                return;
            }
        }

        private void DrawFooter(Graphics g)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(250, 12, 15, 22)))
                g.FillRectangle(b, _rFooter);

            string hint;
            if (_data.Projects.Count == 0)
            {
                hint = "先新建一个项目，用颜色和名称标记你的 DDL";
            }
            else if (_follow)
            {
                hint = "滚轮向下浏览未来   ·   按住图表上下拖动   ·   右下滑块调纵轴";
            }
            else
            {
                hint = "已向下浏览 " + Ui.FmtRemainHM((long)(_scrollY / _pxh * 3600000.0)) +
                    "   ·   滚轮继续 / 回到现在";
            }
            Gfx.TextL(g, hint, Fonts.Tiny, Pal.Muted,
                new RectangleF(Ui.D(22), _rFooter.Top,
                    Math.Max(Ui.D(200), _rFollow.Left - Ui.D(34)), _rFooter.Height));

            bool followHover = Hover(_rFollow);
            bool followActive = _follow;
            if (followHover)
                Gfx.FillRound(g, _rFollow, Ui.DF(7f), Pal.PanelHi);
            else if (followActive)
                Gfx.FillRound(g, _rFollow, Ui.DF(7f), Ui.Alpha(Pal.Green, 12));
            Gfx.Dot(g, _rFollow.X + Ui.D(14), _rFollow.Y + _rFollow.Height / 2f, Ui.DF(2.6f),
                followActive ? Pal.Green : Pal.AccentLight);
            Gfx.TextL(g, followActive ? "实时置顶" : "回到现在", Fonts.Small,
                followActive ? Pal.TextSoft : Pal.AccentLight,
                new RectangleF(_rFollow.X + Ui.D(24), _rFollow.Y, _rFollow.Width - Ui.D(30), _rFollow.Height));

            DrawVerticalScaleSlider(g);
        }

        private void DrawHorizontalScroll(Graphics g)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(250, 11, 15, 22)))
                g.FillRectangle(b, _rHScroll);
            Gfx.HLine(g, 0, ClientSize.Width, _rHScroll.Top, Color.FromArgb(220, 27, 34, 47), 1);

            int n = _data.Projects.Count;
            int visible = Math.Min(n, VisibleProjects);
            string label = n == 0
                ? "项目 0/0"
                : "项目 " + (_firstProject + 1) + "-" + (_firstProject + visible) + "/" + n;
            Gfx.TextL(g, label, Fonts.Tiny, Pal.Muted,
                new RectangleF(Ui.D(10), _rHScroll.Top, _gutterW - Ui.D(16), _rHScroll.Height));

            Rectangle track = _rHScrollTrack;
            Gfx.FillRound(g, new RectangleF(track.X, track.Y, track.Width, track.Height),
                track.Height / 2f, Color.FromArgb(70, 62, 74, 96));

            bool active = HScrollActive;
            if (active)
            {
                bool hover = Hover(_rHScrollThumb) || _hSliderDragging;
                Rectangle thumb = _rHScrollThumb;
                Gfx.FillRound(g, thumb, thumb.Height / 2f,
                    hover ? Color.FromArgb(245, 36, 46, 64) : Color.FromArgb(235, 28, 36, 50));
                Gfx.StrokeRound(g, thumb, thumb.Height / 2f,
                    Ui.Alpha(Pal.Accent, hover ? 235 : 155), 1f);
                for (int i = -1; i <= 1; i++)
                {
                    float gx = thumb.X + thumb.Width / 2f + i * Ui.D(4);
                    using (Pen p = new Pen(Ui.Alpha(Pal.Accent, hover ? 220 : 150), 1f))
                        g.DrawLine(p, gx, thumb.Y + Ui.D(4), gx, thumb.Bottom - Ui.D(4));
                }
            }
        }

        private void DrawVerticalScaleSlider(Graphics g)
        {
            Rectangle group = _rSliderGroup;
            Gfx.FillRound(g, group, Ui.DF(7f), Color.FromArgb(225, 18, 23, 33));
            Gfx.StrokeRound(g, group, Ui.DF(7f),
                Hover(group) || _sliderDragging ? Ui.Alpha(Pal.Accent, 90) : Ui.Alpha(Pal.Border, 170), 1f);
            Gfx.TextL(g, "纵轴比例", Fonts.Tiny, Pal.Muted,
                new RectangleF(group.X + Ui.D(10), group.Y, Ui.D(56), group.Height));

            Rectangle track = _rSliderTrack;
            Gfx.FillRound(g, new RectangleF(track.X, track.Y + track.Height / 2f - 1.5f,
                track.Width, 3f), 1.5f, Color.FromArgb(70, 74, 86, 108));
            float t = SliderT(_pxh);
            float activeW = track.Width * t;
            if (activeW > 1f)
            {
                using (LinearGradientBrush b = new LinearGradientBrush(
                    new PointF(track.X, 0), new PointF(track.X + track.Width, 0),
                    Ui.Alpha(Pal.Accent, 150), Ui.Alpha(Pal.AccentLight, 230)))
                    Gfx.FillRound(g, new RectangleF(track.X, track.Y + track.Height / 2f - 1.5f,
                        activeW, 3f), 1.5f, b);
            }

            for (int i = 0; i <= 4; i++)
            {
                float tx = track.X + track.Width * i / 4f;
                Gfx.Dot(g, tx, track.Y + track.Height / 2f, 1.2f,
                    Color.FromArgb(i == 0 || i == 4 ? 120 : 80, 170, 182, 205));
            }

            RectangleF thumb = _rSliderThumb;
            bool hover = Hover(_rSliderGroup) || _sliderDragging;
            Gfx.Glow(g, new RectangleF(thumb.X - 5, thumb.Y - 5, thumb.Width + 10, thumb.Height + 10),
                Pal.Accent, hover ? 75 : 35);
            Gfx.FillRound(g, thumb, thumb.Width / 2f, hover ? Pal.White : Pal.TextSoft);
            Gfx.StrokeRound(g, thumb, thumb.Width / 2f, Ui.Alpha(Pal.Accent, 230), 1.6f);
            Gfx.Dot(g, thumb.X + thumb.Width / 2f, thumb.Y + thumb.Height / 2f, 2.4f, Pal.Accent);

            string val = VisibleSpanLabel();
            Gfx.TextR(g, val, Fonts.Tiny, hover ? Pal.Text : Pal.Dim,
                new RectangleF(group.Right - Ui.D(60), group.Y, Ui.D(58), group.Height));
        }

        private void DrawToast(Graphics g)
        {
            if (string.IsNullOrEmpty(_toastText)) return;
            long now = EnvironmentTick();
            if (now >= _toastUntil) return;
            long remain = _toastUntil - now;
            int alpha = remain < 420 ? (int)(remain * 255 / 420) : 255;
            if (alpha <= 0) return;

            SizeF sz = Gfx.Measure(g, _toastText, Fonts.Small);
            int w = (int)sz.Width + Ui.D(56);
            int h = Ui.D(34);
            int x = (ClientSize.Width - w) / 2;
            int y = _rFooter.Top - Ui.D(50);
            RectangleF r = new RectangleF(x, y, w, h);
            Gfx.FillRound(g, new RectangleF(x + 2, y + 3, w, h), Ui.DF(17f), Color.FromArgb(alpha * 60 / 255, 0, 0, 0));
            Gfx.FillRound(g, r, Ui.DF(17f), Color.FromArgb(alpha * 242 / 255, 24, 31, 44));
            Gfx.StrokeRound(g, r, Ui.DF(17f), Color.FromArgb(alpha * 70 / 255, 120, 135, 160), 1f);
            Gfx.Dot(g, r.X + Ui.D(17), r.Y + h / 2f, Ui.DF(3.2f),
                Color.FromArgb(alpha, _toastColor));
            Gfx.TextL(g, _toastText, Fonts.Small, Color.FromArgb(alpha, Pal.Text),
                new RectangleF(r.X + Ui.D(28), r.Y, r.Width - Ui.D(38), h));
        }

        private long EnvironmentTick()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private void DrawWindowBorder(Graphics g)
        {
            if (WindowState == FormWindowState.Maximized) return;
            using (Pen p = new Pen(Color.FromArgb(210, 38, 46, 60), 1f))
                g.DrawRectangle(p, 0.5f, 0.5f, ClientSize.Width - 1f, ClientSize.Height - 1f);
        }
    }
}
