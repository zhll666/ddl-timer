using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace ChronoRuler
{
    internal abstract class ThemedDialog : Form
    {
        private bool _dragging;
        private Point _dragScreen;
        private Point _dragLocation;

        protected bool HoverClose;

        protected ThemedDialog()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Pal.Panel;
            DoubleBuffered = true;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.None;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;
                return cp;
            }
        }

        protected Rectangle CloseRect
        {
            get { return new Rectangle(ClientSize.Width - Ui.D(40), Ui.D(10), Ui.D(30), Ui.D(30)); }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Chrome.Apply(Handle);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Pal.Panel);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (CloseRect.Contains(e.Location))
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }
            if (e.Y < Ui.D(52))
            {
                _dragging = true;
                _dragScreen = Cursor.Position;
                _dragLocation = Location;
                Capture = true;
                return;
            }
            ContentMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                Point p = Cursor.Position;
                Location = new Point(_dragLocation.X + p.X - _dragScreen.X,
                    _dragLocation.Y + p.Y - _dragScreen.Y);
                return;
            }
            bool hc = CloseRect.Contains(e.Location);
            if (hc != HoverClose)
            {
                HoverClose = hc;
                Invalidate();
            }
            ContentMouseMove(e);
            if (hc) Cursor = Cursors.Hand;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoverClose = false;
            Invalidate();
        }

        protected virtual void ContentMouseDown(MouseEventArgs e)
        {
        }

        protected virtual void ContentMouseMove(MouseEventArgs e)
        {
        }

        protected abstract void Submit();

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == Keys.Enter)
            {
                Submit();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected void DrawDialogFrame(Graphics g)
        {
            using (Pen p = new Pen(Pal.Border, 1f))
                g.DrawRectangle(p, 0.5f, 0.5f, ClientSize.Width - 1f, ClientSize.Height - 1f);

            Rectangle cr = CloseRect;
            if (HoverClose)
                Gfx.FillRound(g, cr, Ui.DF(6f), Pal.PanelHi);
            Gfx.CloseX(g, new RectangleF(cr.X + Ui.D(7), cr.Y + Ui.D(7), cr.Width - Ui.D(14), cr.Height - Ui.D(14)),
                HoverClose ? Pal.Text : Pal.Dim, Ui.DF(1.2f));
        }

        protected void DrawDialogButton(Graphics g, Rectangle r, string text, bool primary, bool hover, bool danger)
        {
            if (primary)
            {
                Color top = danger ? Ui.Lighten(Pal.Danger, 0.05f) : Pal.Accent;
                Color bottom = danger ? Ui.Darken(Pal.Danger, 0.14f) : Ui.Darken(Pal.Accent, 0.14f);
                if (hover)
                {
                    top = Ui.Lighten(top, 0.10f);
                    bottom = Ui.Lighten(bottom, 0.03f);
                }
                using (LinearGradientBrush b = new LinearGradientBrush(
                    new Point(r.X, r.Y), new Point(r.X, r.Bottom), top, bottom))
                    Gfx.FillRound(g, r, Ui.DF(8f), b);
                Gfx.StrokeRound(g, r, Ui.DF(8f), Ui.Alpha(Color.White, 22), 1f);
                Gfx.TextC(g, text, Fonts.BodyBold, Color.White, r);
            }
            else
            {
                if (hover)
                    Gfx.FillRound(g, r, Ui.DF(8f), Pal.PanelHi);
                Gfx.StrokeRound(g, r, Ui.DF(8f), HoverOrBorder(hover), 1f);
                Gfx.TextC(g, text, Fonts.Body, hover ? Pal.Text : Pal.TextSoft, r);
            }
        }

        private static Color HoverOrBorder(bool hover)
        {
            return hover ? Pal.Border : Ui.Alpha(Pal.Border, 230);
        }

        protected void DrawField(Graphics g, Rectangle r, bool focused)
        {
            Gfx.FillRound(g, r, Ui.DF(9f), Pal.Field);
            Gfx.StrokeRound(g, r, Ui.DF(9f),
                focused ? Ui.Alpha(Pal.Accent, 170) : Ui.Alpha(Pal.Border, 235), focused ? Ui.DF(1.4f) : 1f);
        }

        protected static void DrawWrapped(Graphics g, string text, Font f, Color c, RectangleF r)
        {
            using (SolidBrush b = new SolidBrush(c))
            using (StringFormat sf = new StringFormat())
            {
                sf.Trimming = StringTrimming.None;
                sf.FormatFlags = 0;
                g.DrawString(text, f, b, r, sf);
            }
        }
    }

    internal class ProjectDialog : ThemedDialog
    {
        private readonly Project _existing;
        private readonly TextBox _name;
        private readonly TextBox _time;
        private bool _hoverCancel;
        private bool _hoverOk;
        private int _hoverQuick = -1;
        private int _dragChannel = -1;
        private string _error = "";
        private Rectangle _rNameField;
        private Rectangle _rTimeField;
        private Rectangle _rCancel;
        private Rectangle _rOk;
        private Rectangle _rQuick;
        private Rectangle _rTrackR;
        private Rectangle _rTrackG;
        private Rectangle _rTrackB;
        private int _r;
        private int _g;
        private int _b;

        public string ProjectName { get; private set; }
        public DateTime EndValue { get; private set; }

        public Color SelectedColor
        {
            get { return Color.FromArgb(_r, _g, _b); }
        }

        public ProjectDialog(Project existing, string name, Color color, DateTime end)
        {
            _existing = existing;
            _r = color.R;
            _g = color.G;
            _b = color.B;
            ProjectName = name;
            ClientSize = new Size(Ui.D(460), Ui.D(572));
            Text = existing == null ? "新建项目" : "编辑项目";

            _rNameField = new Rectangle(Ui.D(24), Ui.D(102), Ui.D(412), Ui.D(44));
            _rTimeField = new Rectangle(Ui.D(24), Ui.D(176), Ui.D(412), Ui.D(58));
            _rQuick = new Rectangle(Ui.D(24), Ui.D(266), Ui.D(412), Ui.D(30));
            _rTrackR = new Rectangle(Ui.D(58), Ui.D(348), Ui.D(270), Ui.D(4));
            _rTrackG = new Rectangle(Ui.D(58), Ui.D(382), Ui.D(270), Ui.D(4));
            _rTrackB = new Rectangle(Ui.D(58), Ui.D(416), Ui.D(270), Ui.D(4));
            _rOk = new Rectangle(ClientSize.Width - Ui.D(24) - Ui.D(106), Ui.D(502), Ui.D(106), Ui.D(38));
            _rCancel = new Rectangle(_rOk.Left - Ui.D(10) - Ui.D(88), Ui.D(502), Ui.D(88), Ui.D(38));

            _name = new TextBox();
            _name.BorderStyle = BorderStyle.None;
            _name.BackColor = Pal.Field;
            _name.ForeColor = Pal.Text;
            _name.Font = Fonts.H2;
            _name.Text = name;
            _name.Location = new Point(Ui.D(38), Ui.D(112));
            _name.Size = new Size(Ui.D(364), Ui.D(28));
            _name.MaxLength = 24;
            _name.Enter += delegate { Invalidate(); };
            _name.Leave += delegate { Invalidate(); };
            _name.TextChanged += delegate
            {
                if (_error.Length > 0) _error = "";
                Invalidate();
            };
            Controls.Add(_name);

            _time = new TextBox();
            _time.BorderStyle = BorderStyle.None;
            _time.BackColor = Pal.Field;
            _time.ForeColor = Pal.Text;
            _time.Font = Fonts.TimeInput;
            _time.Text = end.ToString("yyyy-MM-dd HH:mm");
            _time.Location = new Point(Ui.D(38), Ui.D(188));
            _time.Size = new Size(Ui.D(364), Ui.D(36));
            _time.MaxLength = 24;
            _time.Enter += delegate { Invalidate(); };
            _time.Leave += delegate { Invalidate(); };
            _time.TextChanged += delegate
            {
                if (_error.Length > 0) _error = "";
                Invalidate();
            };
            Controls.Add(_time);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _name.Focus();
            _name.SelectAll();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color accent = SelectedColor;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Point(0, 0), new Point(ClientSize.Width, 0),
                Ui.Alpha(accent, 225), Ui.Alpha(accent, 0)))
                g.FillRectangle(b, 0, 0, ClientSize.Width, Ui.D(3));

            Gfx.TextLT(g, _existing == null ? "新建项目" : "编辑项目", Fonts.H1, Pal.Text,
                new RectangleF(Ui.D(24), Ui.D(20), Ui.D(320), Ui.D(28)));
            Gfx.TextLT(g, _existing == null
                    ? "为 DDL 起名、设定截止时间，并调出专属颜色。"
                    : "编辑名称、颜色与截止时间。",
                Fonts.Small, Pal.Dim,
                new RectangleF(Ui.D(24), Ui.D(51), Ui.D(412), Ui.D(20)));

            Gfx.TextLT(g, "项目名称", Fonts.TinyBold, Pal.Dim,
                new RectangleF(Ui.D(24), Ui.D(84), Ui.D(220), Ui.D(16)));
            DrawField(g, _rNameField, _name.Focused);

            Gfx.TextLT(g, "结束时间 · 年-月-日 时:分", Fonts.TinyBold, Pal.Dim,
                new RectangleF(Ui.D(24), Ui.D(158), Ui.D(220), Ui.D(16)));
            DrawField(g, _rTimeField, _time.Focused);

            DateTime target;
            bool valid = TryParseTarget(out target);
            DateTime now = DateTime.Now;
            if (valid)
            {
                long remain = Ui.ToMs(target) - Ui.ToMs(now);
                string date = target.ToString("yyyy年M月d日") + " " + Ui.Weekday(target) +
                    " " + target.ToString("HH:mm");
                string preview;
                Color color;
                if (remain > 0)
                {
                    preview = date + "  ·  剩余 " + Ui.FmtRemain(remain);
                    color = target.Date != now.Date ? Pal.AccentLight : Pal.Muted;
                }
                else
                {
                    preview = date + "  ·  需要晚于现在";
                    color = Pal.Coral;
                }
                Gfx.TextLT(g, preview, Fonts.Tiny, color,
                    new RectangleF(Ui.D(24), Ui.D(240), Ui.D(412), Ui.D(16)));
            }
            else
            {
                Gfx.TextLT(g, "格式示例：2026-11-23 05:30", Fonts.Tiny, Pal.Muted,
                    new RectangleF(Ui.D(24), Ui.D(240), Ui.D(412), Ui.D(16)));
            }

            DrawQuickChips(g);

            Gfx.TextLT(g, "项目颜色", Fonts.TinyBold, Pal.Dim,
                new RectangleF(Ui.D(24), Ui.D(308), Ui.D(160), Ui.D(16)));
            Gfx.TextR(g, "#" + _r.ToString("X2") + _g.ToString("X2") + _b.ToString("X2"),
                Fonts.Tiny, Pal.TextSoft,
                new RectangleF(Ui.D(286), Ui.D(307), Ui.D(150), Ui.D(16)));

            DrawChannel(g, _rTrackR, _r, Color.FromArgb(255, 104, 112), "R");
            DrawChannel(g, _rTrackG, _g, Color.FromArgb(88, 220, 156), "G");
            DrawChannel(g, _rTrackB, _b, Color.FromArgb(98, 152, 255), "B");

            RectangleF sw = new RectangleF(Ui.D(382), Ui.D(332), Ui.D(52), Ui.D(52));
            Gfx.FillRound(g, sw, Ui.DF(9f), accent);
            Gfx.StrokeRound(g, sw, Ui.DF(9f), Ui.Alpha(Color.White, 65), 1f);
            Gfx.TextC(g, "颜色", Fonts.Tiny, Ui.Alpha(Color.White, 230), sw);

            Gfx.TextLT(g, "拖动 R / G / B 滑块自定义颜色；结束时间需要晚于现在。",
                Fonts.Tiny, Pal.Muted,
                new RectangleF(Ui.D(24), Ui.D(440), Ui.D(412), Ui.D(16)));

            if (_error.Length > 0)
                Gfx.TextLT(g, _error, Fonts.Small, Pal.Danger,
                    new RectangleF(Ui.D(24), Ui.D(460), Ui.D(412), Ui.D(18)));

            DrawDialogButton(g, _rCancel, "取消", false, _hoverCancel, false);
            DrawDialogButton(g, _rOk, _existing == null ? "创建项目" : "保存", true, _hoverOk, false);
            DrawDialogFrame(g);
        }

        private void DrawChannel(Graphics g, Rectangle track, int value, Color color, string label)
        {
            int cy = track.Y + track.Height / 2;
            Gfx.TextL(g, label, Fonts.SmallBold, color,
                new RectangleF(Ui.D(24), cy - 10, Ui.D(22), Ui.D(20)));

            RectangleF baseR = new RectangleF(track.X, track.Y, track.Width, track.Height);
            Gfx.FillRound(g, baseR, track.Height / 2f, Color.FromArgb(255, 8, 11, 17));
            float t = value / 255f;
            float fillW = Math.Max(0, track.Width * t);
            if (fillW > 0)
                Gfx.FillRound(g, new RectangleF(track.X, track.Y, fillW, track.Height),
                    track.Height / 2f, Ui.Alpha(color, 210));
            Gfx.StrokeRound(g, baseR, track.Height / 2f, Ui.Alpha(Pal.Border, 210), 1f);

            float thumbX = track.X + track.Width * t;
            Gfx.FillRound(g, new RectangleF(thumbX - Ui.DF(6f), cy - Ui.DF(6f), Ui.DF(12f), Ui.DF(12f)),
                Ui.DF(6f), color);
            Gfx.StrokeRound(g, new RectangleF(thumbX - Ui.DF(6f), cy - Ui.DF(6f), Ui.DF(12f), Ui.DF(12f)),
                Ui.DF(6f), Color.FromArgb(230, 250, 252, 255), 1f);

            Gfx.TextR(g, value.ToString(), Fonts.Tiny, Pal.TextSoft,
                new RectangleF(Ui.D(338), cy - 9, Ui.D(32), Ui.D(18)));
        }

        private void DrawQuickChips(Graphics g)
        {
            string[] labels = { "30 分钟后", "2 小时后", "明天此刻", "3 天后" };
            for (int i = 0; i < labels.Length; i++)
            {
                Rectangle r = QuickRect(i);
                bool hover = _hoverQuick == i;
                Gfx.FillRound(g, r, Ui.DF(7f), hover ? Pal.PanelHi : Color.FromArgb(210, 19, 25, 36));
                Gfx.StrokeRound(g, r, Ui.DF(7f),
                    hover ? Ui.Alpha(Pal.Accent, 120) : Ui.Alpha(Pal.Border, 190), 1f);
                Gfx.TextC(g, labels[i], Fonts.Tiny, hover ? Pal.Text : Pal.Dim, r);
            }
        }

        private Rectangle QuickRect(int index)
        {
            return new Rectangle(_rQuick.X + index * Ui.D(100), _rQuick.Y, Ui.D(94), Ui.D(30));
        }

        protected override void ContentMouseDown(MouseEventArgs e)
        {
            if (_rCancel.Contains(e.Location))
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }
            if (_rOk.Contains(e.Location))
            {
                Submit();
                return;
            }
            int q = QuickAt(e.Location);
            if (q >= 0)
            {
                DateTime t = DateTime.Now;
                if (q == 0) t = t.AddMinutes(30);
                else if (q == 1) t = t.AddHours(2);
                else if (q == 2) t = t.AddDays(1);
                else t = t.AddDays(3);
                t = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0);
                _time.Text = t.ToString("yyyy-MM-dd HH:mm");
                _time.Focus();
                _time.SelectAll();
                Invalidate();
                return;
            }
            int ch = ChannelAt(e.Location);
            if (ch >= 0)
            {
                _dragChannel = ch;
                SetChannelFromX(ch, e.X);
                Capture = true;
                Invalidate();
                return;
            }
            if (_rNameField.Contains(e.Location)) _name.Focus();
            else if (_rTimeField.Contains(e.Location)) _time.Focus();
        }

        protected override void ContentMouseMove(MouseEventArgs e)
        {
            if (_dragChannel >= 0 && (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left)
            {
                SetChannelFromX(_dragChannel, e.X);
                return;
            }

            int q = QuickAt(e.Location);
            if (q != _hoverQuick) { _hoverQuick = q; Invalidate(); }
            bool hc = _rCancel.Contains(e.Location);
            bool ho = _rOk.Contains(e.Location);
            if (hc != _hoverCancel || ho != _hoverOk)
            {
                _hoverCancel = hc;
                _hoverOk = ho;
                Invalidate();
            }
            bool slider = ChannelAt(e.Location) >= 0;
            Cursor = (q >= 0 || hc || ho) ? Cursors.Hand : (slider ? Cursors.SizeWE : Cursors.Default);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragChannel >= 0)
            {
                _dragChannel = -1;
                Capture = false;
                Invalidate();
            }
        }

        private int ChannelAt(Point p)
        {
            if (NearTrack(_rTrackR, p)) return 0;
            if (NearTrack(_rTrackG, p)) return 1;
            if (NearTrack(_rTrackB, p)) return 2;
            return -1;
        }

        private static bool NearTrack(Rectangle track, Point p)
        {
            Rectangle hit = new Rectangle(track.X - Ui.D(6), track.Y - Ui.D(11),
                track.Width + Ui.D(12), track.Height + Ui.D(22));
            return hit.Contains(p);
        }

        private void SetChannelFromX(int channel, int x)
        {
            Rectangle track = channel == 0 ? _rTrackR : (channel == 1 ? _rTrackG : _rTrackB);
            float t = (float)(x - track.X) / Math.Max(1, track.Width);
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            int v = (int)Math.Round(t * 255);
            if (channel == 0) _r = v;
            else if (channel == 1) _g = v;
            else _b = v;
            Invalidate();
        }

        private int QuickAt(Point p)
        {
            for (int i = 0; i < 4; i++)
                if (QuickRect(i).Contains(p)) return i;
            return -1;
        }

        protected override void Submit()
        {
            string name = (_name.Text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            if (name.Length == 0)
            {
                _error = "项目名称不能为空";
                _name.Focus();
                Invalidate();
                return;
            }
            if (name.Length > 24)
            {
                _error = "项目名称最多 24 个字符";
                _name.Focus();
                Invalidate();
                return;
            }

            DateTime target;
            if (!TryParseTarget(out target))
            {
                _error = "格式不正确，请按 2026-11-23 05:30 输入";
                _time.Focus();
                Invalidate();
                return;
            }
            if (target <= DateTime.Now.AddSeconds(5))
            {
                _error = "结束时间需要晚于当前时间";
                _time.Focus();
                Invalidate();
                return;
            }
            ProjectName = name;
            EndValue = target;
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool TryParseTarget(out DateTime target)
        {
            target = DateTime.MinValue;
            string[] formats =
            {
                "yyyy-MM-dd H:mm", "yyyy-MM-dd HH:mm",
                "yyyy/M/d H:mm", "yyyy/M/d HH:mm",
                "yyyy-M-d H:mm", "yyyy-M-d HH:mm",
                "yyyy年M月d日 H:mm", "yyyy年M月d日 HH:mm"
            };
            DateTime t;
            if (!DateTime.TryParseExact(_time.Text.Trim(), formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out t))
                return false;
            target = t;
            return true;
        }
    }

    internal class ConfirmDialog : ThemedDialog
    {
        private readonly string _message;
        private readonly string _confirmText;
        private readonly bool _danger;
        private bool _hoverCancel;
        private bool _hoverOk;
        private Rectangle _rCancel;
        private Rectangle _rOk;

        private ConfirmDialog(string title, string message, string confirmText, bool danger)
        {
            _message = message;
            _confirmText = confirmText;
            _danger = danger;
            ClientSize = new Size(Ui.D(440), Ui.D(276));
            Text = title;
            _rOk = new Rectangle(ClientSize.Width - Ui.D(24) - Ui.D(112), Ui.D(216), Ui.D(112), Ui.D(38));
            _rCancel = new Rectangle(_rOk.Left - Ui.D(10) - Ui.D(88), Ui.D(216), Ui.D(88), Ui.D(38));
            _title = title;
        }

        private readonly string _title;

        public static bool Ask(IWin32Window owner, string title, string message, string confirmText, bool danger)
        {
            using (ConfirmDialog d = new ConfirmDialog(title, message, confirmText, danger))
            {
                return d.ShowDialog(owner) == DialogResult.OK;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color accent = _danger ? Pal.Danger : Pal.Accent;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Point(0, 0), new Point(ClientSize.Width, 0), Ui.Alpha(accent, 180), Ui.Alpha(accent, 0)))
                g.FillRectangle(b, 0, 0, ClientSize.Width, Ui.D(3));

            RectangleF icon = new RectangleF(Ui.D(24), Ui.D(24), Ui.D(42), Ui.D(42));
            Gfx.FillRound(g, icon, Ui.DF(13f), Ui.Alpha(accent, 18));
            Gfx.StrokeRound(g, icon, Ui.DF(13f), Ui.Alpha(accent, 80), 1f);
            Gfx.TextC(g, "!", Fonts.H2, accent, icon);

            Gfx.TextLT(g, _title, Fonts.H2, Pal.Text,
                new RectangleF(Ui.D(78), Ui.D(22), Ui.D(320), Ui.D(26)));
            Gfx.TextLT(g, "请确认这次操作", Fonts.Tiny, Pal.Muted,
                new RectangleF(Ui.D(78), Ui.D(46), Ui.D(320), Ui.D(16)));

            DrawWrapped(g, _message, Fonts.Body, Pal.TextSoft,
                new RectangleF(Ui.D(24), Ui.D(86), Ui.D(392), Ui.D(108)));

            DrawDialogButton(g, _rCancel, "取消", false, _hoverCancel, false);
            DrawDialogButton(g, _rOk, _confirmText, true, _hoverOk, _danger);
            DrawDialogFrame(g);
        }

        protected override void ContentMouseDown(MouseEventArgs e)
        {
            if (_rCancel.Contains(e.Location))
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }
            if (_rOk.Contains(e.Location))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        protected override void ContentMouseMove(MouseEventArgs e)
        {
            bool hc = _rCancel.Contains(e.Location);
            bool ho = _rOk.Contains(e.Location);
            if (hc != _hoverCancel || ho != _hoverOk)
            {
                _hoverCancel = hc;
                _hoverOk = ho;
                Invalidate();
            }
            Cursor = (hc || ho) ? Cursors.Hand : Cursors.Default;
        }

        protected override void Submit()
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
