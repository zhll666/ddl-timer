using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ChronoRuler
{
    public partial class MainForm : Form
    {
        private AppData _data;
        private bool _follow = true;
        private readonly string _screenshotPath;

        private readonly System.Windows.Forms.Timer _tick;
        private DateTime _lastSecond;
        private DateTime _now = DateTime.Now;

        private Point _mouse = new Point(-1000, -1000);
        private bool _mouseInside;
        private bool _draggingWindow;
        private bool _dragMoved;
        private Point _dragScreenStart;
        private Point _dragFormStart;

        private bool _draggingChart;
        private float _dragChartStartY;
        private double _dragChartStartScroll;
        private bool _sliderDragging;

        private string _toastText;
        private Color _toastColor;
        private long _toastUntil;
        private int _toastSeq;

        // layout (device pixels)
        private Rectangle _rTitle;
        private Rectangle _rHeader;
        private Rectangle _rRuler;
        private Rectangle _rTimeline;
        private Rectangle _rFooter;
        private Rectangle _rGutter;
        private Rectangle _rCols;
        private Rectangle _rBtnMin;
        private Rectangle _rBtnMax;
        private Rectangle _rBtnClose;
        private Rectangle _rLogo;
        private Rectangle _rClock;
        private Rectangle _rMinuteBar;
        private Rectangle _rNew;
        private Rectangle _rFollow;
        private Rectangle _rSliderGroup;
        private Rectangle _rSliderTrack;
        private Rectangle _rSliderThumb;
        private int _gutterW;
        private int _colX0;
        private int _colRight;
        private List<Rectangle> _colRects = new List<Rectangle>();
        private float _pxh = 120f;
        private double _scrollY;

        private const int ResizeBorder = 6;
        private const int WM_NCHITTEST = 0x0084;

        public MainForm(string screenshotPath)
        {
            _screenshotPath = screenshotPath;
            _data = Store.Load();
            _pxh = (float)_data.PxHour;
            _now = DateTime.Now;

            Text = "ddl计时器";
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Pal.Bg;
            MinimumSize = new Size(Ui.D(1000), Ui.D(660));
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int w = Math.Min(Ui.D(1240), Math.Max(Ui.D(1000), wa.Width - Ui.D(60)));
            int h = Math.Min(Ui.D(820), Math.Max(Ui.D(680), wa.Height - Ui.D(60)));
            ClientSize = new Size(w, h);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(wa.X + (wa.Width - w) / 2, wa.Y + (wa.Height - h) / 2);

            ComputeLayout();
            if (_follow) _scrollY = 0;

            _tick = new System.Windows.Forms.Timer();
            _tick.Interval = 40;
            _tick.Tick += OnTick;
            _tick.Start();

            if (!string.IsNullOrEmpty(_screenshotPath))
            {
                System.Windows.Forms.Timer shot = new System.Windows.Forms.Timer();
                shot.Interval = 1100;
                shot.Tick += delegate
                {
                    shot.Stop();
                    shot.Dispose();
                    CaptureScreenshot();
                };
                shot.Start();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Chrome.Apply(Handle);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            MaxmizeBoundsForScreen();
            ComputeLayout();
            ExpireProjects(true);
            if (_follow) _scrollY = 0;
            Invalidate();
        }

        private void MaxmizeBoundsForScreen()
        {
            Screen scr = Screen.FromControl(this);
            MaximizedBounds = scr.WorkingArea;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!string.IsNullOrEmpty(_screenshotPath))
            {
                _tick.Stop();
                base.OnFormClosing(e);
                return;
            }
            ExpireProjects(false);
            _data.PxHour = _pxh;
            _data.Zoom = _pxh <= 45 ? 0 : (_pxh <= 220 ? 1 : 2);
            Store.Save(_data);
            _tick.Stop();
            base.OnFormClosing(e);
        }

        private void CaptureScreenshot()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(ClientSize.Width, ClientSize.Height))
                {
                    DrawToBitmap(bmp, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
                    bmp.Save(_screenshotPath, ImageFormat.Png);
                }
            }
            catch
            {
            }
            _tick.Stop();
            Close();
        }

        // ---------------------------------------------------------------- clock

        private void OnTick(object sender, EventArgs e)
        {
            _now = DateTime.Now;
            ExpireProjects(true);
            ComputeLayout();

            // The future timeline is pinned to "now" at the top while following.
            if (_follow) _scrollY = 0;

            bool toast = NowTick() < _toastUntil;
            int target = toast ? 40 : 500;
            if (_tick.Interval != target) _tick.Interval = target;

            if (toast || _draggingChart || _sliderDragging ||
                (DateTime.Now.Second != _lastSecond.Second) || _mouseInside)
            {
                if (DateTime.Now.Second != _lastSecond.Second) _lastSecond = DateTime.Now;
                Invalidate();
            }
        }

        // ---------------------------------------------------------------- layout

        private void ComputeLayout()
        {
            if (_data == null) return;
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            int titleH = Ui.D(42);
            int headerH = Ui.D(148);
            int rulerH = Ui.D(74);
            int footerH = Ui.D(38);

            _rTitle = new Rectangle(0, 0, w, titleH);
            _rHeader = new Rectangle(0, titleH, w, headerH);
            _rRuler = new Rectangle(0, _rHeader.Bottom, w, rulerH);
            _rFooter = new Rectangle(0, h - footerH, w, footerH);
            _rTimeline = new Rectangle(0, _rRuler.Bottom, w, Math.Max(0, _rFooter.Top - _rRuler.Bottom));

            _gutterW = Ui.D(96);
            _rGutter = new Rectangle(0, _rTimeline.Top, _gutterW, _rTimeline.Height);
            _colX0 = _gutterW;
            _colRight = w - Ui.D(22);
            _rCols = new Rectangle(_colX0, _rTimeline.Top, Math.Max(0, _colRight - _colX0), _rTimeline.Height);

            int btnW = Ui.D(46);
            _rBtnClose = new Rectangle(w - btnW, 0, btnW, titleH);
            _rBtnMax = new Rectangle(w - btnW * 2, 0, btnW, titleH);
            _rBtnMin = new Rectangle(w - btnW * 3, 0, btnW, titleH);
            _rLogo = Ui.R(22, 9, 24, 24);

            // one-minute duty cycle reader, pinned to the top of the workspace
            _rMinuteBar = new Rectangle(Ui.D(158), _rHeader.Top + Ui.D(12),
                Math.Max(Ui.D(300), w - Ui.D(158) - Ui.D(24)), Ui.D(24));
            _rClock = new Rectangle(Ui.D(22), _rHeader.Top + Ui.D(44), Ui.D(320), Ui.D(48));

            int pad = Ui.D(24);
            int newW = Ui.D(132);
            _rNew = new Rectangle(w - pad - newW, _rHeader.Top + Ui.D(66), newW, Ui.D(44));

            int sliderW = Ui.D(264);
            int sliderH = Ui.D(26);
            _rSliderGroup = new Rectangle(w - pad - sliderW, _rFooter.Top + (footerH - sliderH) / 2,
                sliderW, sliderH);
            _rFollow = new Rectangle(_rSliderGroup.Left - Ui.D(16) - Ui.D(92),
                _rFooter.Top + (footerH - sliderH) / 2, Ui.D(92), sliderH);
            _rSliderTrack = new Rectangle(_rSliderGroup.X + Ui.D(66), _rSliderGroup.Y + Ui.D(12),
                Ui.D(136), Ui.D(4));

            // columns
            _colRects.Clear();
            int n = _data.Projects.Count;
            if (n > 0)
            {
                int span = _colRight - _colX0;
                int prev = 0;
                for (int i = 0; i < n; i++)
                {
                    int next = (int)Math.Round((double)span * (i + 1) / n);
                    Rectangle cr = new Rectangle(_colX0 + prev, _rTimeline.Top, next - prev, _rTimeline.Height);
                    _colRects.Add(cr);
                    prev = next;
                }
            }

            float minPx = MinPxHour;
            if (_pxh < minPx) _pxh = minPx;
            if (_pxh > 520f) _pxh = 520f;

            if (_follow) _scrollY = 0;
            if (_scrollY < 0) _scrollY = 0;
            double hardMax = 24.0 * 365.0 * 100.0 * _pxh;
            if (_scrollY > hardMax) _scrollY = hardMax;

            float t = SliderT(_pxh);
            int thumbX = _rSliderTrack.X + (int)Math.Round(t * _rSliderTrack.Width);
            _rSliderThumb = new Rectangle(thumbX - Ui.D(7),
                _rSliderTrack.Y + _rSliderTrack.Height / 2 - Ui.D(7), Ui.D(14), Ui.D(14));

            int cardW = Math.Min(Ui.D(470), Math.Max(Ui.D(330), _rCols.Width - Ui.D(80)));
            int emptyH = Ui.D(268);
            int cx = _rCols.Left + (_rCols.Width - cardW) / 2;
            int cy = _rTimeline.Top + (_rTimeline.Height - emptyH) / 2;
            _rEmptyCard = new Rectangle(cx, cy, cardW, emptyH);
            _rEmptyNew = new Rectangle(cx + Ui.D(32), cy + emptyH - Ui.D(79), Ui.D(148), Ui.D(40));
        }

        private float MinPxHour
        {
            get { return Math.Max(12f, (_rTimeline.Height - Ui.D(24)) / 24f); }
        }

        private float SliderT(float px)
        {
            float min = MinPxHour;
            if (px <= min) return 0f;
            if (px >= 520f) return 1f;
            return (float)(Math.Log(px / min) / Math.Log(520.0 / min));
        }

        private float SliderPx(float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            float min = MinPxHour;
            return (float)(min * Math.Pow(520.0 / min, t));
        }

        private float YFor(DateTime t)
        {
            double hours = (t - _now).TotalHours;
            return _rTimeline.Top + (float)(hours * _pxh) - (float)_scrollY;
        }

        private DateTime TopTime
        {
            get
            {
                if (_pxh <= 0) return _now;
                return _now.AddMilliseconds(_scrollY / _pxh * 3600000.0);
            }
        }

        // ---------------------------------------------------------------- helpers

        private static double Clamp(double v, double lo, double hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        private static long NowTick()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ComputeLayout();
            if (_follow) _scrollY = 0;
            Invalidate();
        }

        private long NowMs
        {
            get { return Ui.NowMs(); }
        }

        private void ShowToast(string text, Color color)
        {
            _toastText = text;
            _toastColor = color;
            _toastUntil = NowTick() + 2600;
            _toastSeq++;
            Invalidate();
        }

        private bool SaveData()
        {
            _data.PxHour = _pxh;
            _data.Zoom = _pxh <= 45 ? 0 : (_pxh <= 220 ? 1 : 2);
            bool ok = Store.Save(_data);
            if (!ok) ShowToast("保存失败，请检查磁盘权限", Pal.Danger);
            return ok;
        }

        private void RenumberProjects()
        {
            for (int i = 0; i < _data.Projects.Count; i++)
                _data.Projects[i].Ordinal = i + 1;
        }

        private int ExpireProjects(bool notify)
        {
            if (_data == null || _data.Projects.Count == 0) return 0;
            long now = NowMs;
            int expired = 0;
            string lastName = "";
            for (int i = _data.Projects.Count - 1; i >= 0; i--)
            {
                Project p = _data.Projects[i];
                if (p == null || p.EndAt > now) continue;
                lastName = string.IsNullOrEmpty(p.Name) ? ("项目 " + p.Ordinal) : p.Name;
                _data.Projects.RemoveAt(i);
                expired++;
            }
            if (expired == 0) return 0;

            RenumberProjects();
            SaveData();
            ComputeLayout();
            if (notify)
            {
                string msg = expired == 1
                    ? "已到期并移出：" + lastName
                    : expired + " 个项目已到期并移出";
                ShowToast(msg, Pal.Coral);
            }
            Invalidate();
            return expired;
        }

        // ---------------------------------------------------------------- actions

        private Color ProjectColor(Project p)
        {
            if (p == null) return Pal.Accent;
            return Color.FromArgb(p.Color);
        }

        private void NewProject()
        {
            DateTime def = DateTime.Now.AddHours(1);
            def = def.AddSeconds(-def.Second);
            int rem = def.Minute % 5;
            if (rem > 0) def = def.AddMinutes(5 - rem);
            using (ProjectDialog dlg = new ProjectDialog(null, NextProjectName(),
                Pal.ProjectColor(_data.Projects.Count), def))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Project p = new Project();
                    p.Id = Guid.NewGuid().ToString("N");
                    p.Name = dlg.ProjectName;
                    p.Color = dlg.SelectedColor.ToArgb();
                    p.Created = NowMs;
                    p.EndAt = Ui.ToMs(dlg.EndValue);
                    p.Ordinal = _data.Projects.Count + 1;
                    _data.Projects.Add(p);
                    SaveData();
                    _follow = true;
                    _scrollY = 0;
                    ComputeLayout();
                    ShowToast("已创建 " + p.Name + " · 截止 " + EndLabel(p.EndAt),
                        ProjectColor(p));
                    Invalidate();
                }
            }
        }

        private string NextProjectName()
        {
            int n = 1;
            while (true)
            {
                string name = "项目 " + n;
                bool used = false;
                for (int i = 0; i < _data.Projects.Count; i++)
                {
                    if (_data.Projects[i].Name == name) { used = true; break; }
                }
                if (!used) return name;
                n++;
            }
        }

        private void EditProject(Project p)
        {
            if (p == null) return;
            DateTime current = DateTimeOffset.FromUnixTimeMilliseconds(p.EndAt).ToLocalTime().DateTime;
            using (ProjectDialog dlg = new ProjectDialog(p, p.Name, ProjectColor(p), current))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (!_data.Projects.Contains(p))
                    {
                        ShowToast("该项目已到期并移出", Pal.Coral);
                        Invalidate();
                        return;
                    }
                    p.Name = dlg.ProjectName;
                    p.Color = dlg.SelectedColor.ToArgb();
                    p.EndAt = Ui.ToMs(dlg.EndValue);
                    SaveData();
                    ShowToast(p.Name + " · 截止 " + EndLabel(p.EndAt), ProjectColor(p));
                    Invalidate();
                }
            }
        }

        private void DeleteProject(Project p)
        {
            if (p == null) return;
            bool ok = ConfirmDialog.Ask(this, "删除项目？",
                "项目“" + p.Name + "”将从图表中永久删除，此操作不可撤销。",
                "删除项目", true);
            if (!ok) return;

            _data.Projects.Remove(p);
            RenumberProjects();
            SaveData();
            ComputeLayout();
            ShowToast("项目已删除，后续项目已前移", Pal.Danger);
            Invalidate();
        }

        private void SetScale(float px, bool persist)
        {
            float min = MinPxHour;
            if (px < min) px = min;
            if (px > 520f) px = 520f;
            if (Math.Abs(px - _pxh) < 0.05f) return;

            double anchor = 0;
            if (_pxh > 0 && _rTimeline.Height > 0)
                anchor = (_scrollY + _rTimeline.Height * 0.5) / _pxh;
            _pxh = px;
            ComputeLayout();
            if (_follow)
            {
                _scrollY = 0;
            }
            else
            {
                _scrollY = Math.Max(0, anchor * _pxh - _rTimeline.Height * 0.5);
            }
            if (persist) SaveData();
            Invalidate();
        }

        private void SetScaleFromX(int x)
        {
            int left = _rSliderTrack.X;
            int width = Math.Max(1, _rSliderTrack.Width);
            float t = (float)(x - left) / width;
            SetScale(SliderPx(t), false);
        }

        private void SetScalePreset(int preset)
        {
            if (preset == 0) SetScale(MinPxHour, true);
            else if (preset == 1) SetScale(120f, true);
            else SetScale(400f, true);
        }

        private void ToggleFollow()
        {
            _follow = true;
            _scrollY = 0;
            ShowToast("已回到现在", Pal.Green);
            Invalidate();
        }

        private void ScrollBy(double delta)
        {
            double next = _scrollY + delta;
            if (next < 0) next = 0;
            double hardMax = 24.0 * 365.0 * 100.0 * _pxh;
            if (next > hardMax) next = hardMax;
            _scrollY = next;
            _follow = _scrollY <= 0.5;
            if (_follow) _scrollY = 0;
            Invalidate();
        }

        // ---------------------------------------------------------------- hit tests

        private bool InContent(Point p)
        {
            return _rTimeline.Contains(p) && p.X >= _colX0 && p.X < _colRight;
        }

        private int ColumnAt(Point p)
        {
            for (int i = 0; i < _colRects.Count; i++)
                if (_colRects[i].Contains(p)) return i;
            return -1;
        }

        private int HeaderAt(Point p)
        {
            if (p.Y < _rRuler.Top || p.Y >= _rRuler.Bottom) return -1;
            for (int i = 0; i < _colRects.Count; i++)
            {
                Rectangle head = new Rectangle(_colRects[i].X, _rRuler.Top, _colRects[i].Width, _rRuler.Height);
                if (head.Contains(p)) return i;
            }
            return -1;
        }

        private bool HitTitleBarButton(Point p)
        {
            return _rBtnMin.Contains(p) || _rBtnMax.Contains(p) || _rBtnClose.Contains(p);
        }

        private bool HitSlider(Point p)
        {
            if (_rSliderGroup.IsEmpty) return false;
            Rectangle hit = new Rectangle(_rSliderTrack.X - Ui.D(10), _rSliderGroup.Y,
                _rSliderTrack.Width + Ui.D(20), _rSliderGroup.Height);
            return hit.Contains(p) || _rSliderThumb.Contains(p);
        }

        // ---------------------------------------------------------------- input

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mouse = e.Location;
            _mouseInside = true;

            if (_draggingWindow)
            {
                Point screen = Cursor.Position;
                int dx = screen.X - _dragScreenStart.X;
                int dy = screen.Y - _dragScreenStart.Y;
                if (!_dragMoved && Math.Abs(dx) + Math.Abs(dy) > 3) _dragMoved = true;
                if (_dragMoved && WindowState == FormWindowState.Normal)
                {
                    Location = new Point(_dragFormStart.X + dx, _dragFormStart.Y + dy);
                }
                return;
            }

            if (_draggingChart)
            {
                double next = _dragChartStartScroll + (_dragChartStartY - e.Y);
                if (next < 0) next = 0;
                _scrollY = next;
                _follow = _scrollY <= 0.5;
                if (_follow) _scrollY = 0;
                Invalidate();
                return;
            }

            if (_sliderDragging)
            {
                SetScaleFromX(e.X);
                return;
            }

            Cursor c = Cursors.Default;
            if (_rBtnMin.Contains(e.Location) || _rBtnMax.Contains(e.Location) || _rBtnClose.Contains(e.Location)) c = Cursors.Hand;
            else if (_rNew.Contains(e.Location)) c = Cursors.Hand;
            else if (_data.Projects.Count == 0 && _rEmptyNew.Contains(e.Location)) c = Cursors.Hand;
            else if (_rFollow.Contains(e.Location)) c = Cursors.Hand;
            else if (HitSlider(e.Location)) c = Cursors.Hand;
            else if (InContent(e.Location)) c = Cursors.SizeNS;
            Cursor = c;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _mouse = new Point(-1000, -1000);
            _mouseInside = false;
            if (!_draggingChart) Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _mouse = e.Location;
            if (e.Button == MouseButtons.Right)
            {
                HandleRightClick(e.Location);
                return;
            }
            if (e.Button != MouseButtons.Left) return;

            if (_rBtnClose.Contains(e.Location))
            {
                Close();
                return;
            }
            if (_rBtnMin.Contains(e.Location))
            {
                WindowState = FormWindowState.Minimized;
                return;
            }
            if (_rBtnMax.Contains(e.Location))
            {
                ToggleMaximize();
                return;
            }
            if (_rNew.Contains(e.Location))
            {
                NewProject();
                return;
            }
            if (_data.Projects.Count == 0 && _rEmptyNew.Contains(e.Location))
            {
                NewProject();
                return;
            }
            if (_rFollow.Contains(e.Location))
            {
                ToggleFollow();
                return;
            }
            if (HitSlider(e.Location))
            {
                _sliderDragging = true;
                Capture = true;
                SetScaleFromX(e.X);
                return;
            }
            if (InContent(e.Location))
            {
                _draggingChart = true;
                _dragChartStartY = e.Y;
                _dragChartStartScroll = _scrollY;
                Capture = true;
                return;
            }
            if (_rTitle.Contains(e.Location) && !HitTitleBarButton(e.Location))
            {
                if (WindowState == FormWindowState.Normal)
                {
                    _draggingWindow = true;
                    _dragMoved = false;
                    _dragScreenStart = Cursor.Position;
                    _dragFormStart = Location;
                    Capture = true;
                }
                return;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_draggingWindow)
            {
                _draggingWindow = false;
                Capture = false;
            }
            if (_draggingChart)
            {
                _draggingChart = false;
                Capture = false;
            }
            if (_sliderDragging)
            {
                _sliderDragging = false;
                Capture = false;
                SaveData();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;
            if (_rTitle.Contains(e.Location) && !HitTitleBarButton(e.Location))
            {
                ToggleMaximize();
                return;
            }
            int header = HeaderAt(e.Location);
            if (header >= 0)
            {
                EditProject(_data.Projects[header]);
                return;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                SetScale(_pxh * (e.Delta > 0 ? 1.15f : 0.87f), true);
                return;
            }
            float step = Math.Max(Ui.D(80), _rTimeline.Height * 0.28f);
            ScrollBy(e.Delta > 0 ? -step : step);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.N)
            {
                NewProject();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.PageDown)
            {
                ScrollBy(Math.Max(Ui.D(120), _rTimeline.Height * 0.35f));
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.PageUp)
            {
                ScrollBy(-Math.Max(Ui.D(120), _rTimeline.Height * 0.35f));
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Home || e.KeyCode == Keys.F)
            {
                ToggleFollow();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
            {
                SetScalePreset(0);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2)
            {
                SetScalePreset(1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3)
            {
                SetScalePreset(2);
                e.Handled = true;
            }
        }

        private void ToggleMaximize()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Normal;
            }
            else
            {
                MaxmizeBoundsForScreen();
                WindowState = FormWindowState.Maximized;
            }
            ComputeLayout();
            Invalidate();
        }

        private void HandleRightClick(Point p)
        {
            for (int i = 0; i < _colRects.Count; i++)
            {
                Rectangle head = new Rectangle(_colRects[i].X, _rRuler.Top, _colRects[i].Width, _rRuler.Height);
                if (_colRects[i].Contains(p) || head.Contains(p))
                {
                    Project project = _data.Projects[i];
                    ContextMenuStrip m = CreateMenu();
                    m.Items.Add(DisabledItem(project.Ordinal + "-（" + project.Name + "） · 截止 " + EndLabel(project.EndAt)));
                    m.Items.Add(new ToolStripSeparator());
                    m.Items.Add(Item("编辑项目…", delegate { EditProject(project); }, false));
                    m.Items.Add(new ToolStripSeparator());
                    m.Items.Add(Item("删除项目", delegate { DeleteProject(project); }, true));
                    m.Show(PointToScreen(p));
                    return;
                }
            }
        }

        // ---------------------------------------------------------------- context menu

        private ContextMenuStrip CreateMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.Font = Fonts.Body;
            m.BackColor = Pal.PanelAlt;
            m.ForeColor = Pal.TextSoft;
            m.ShowImageMargin = false;
            m.ShowCheckMargin = false;
            m.Padding = new Padding(Ui.D(4));
            m.Renderer = new DarkMenuRenderer();
            return m;
        }

        private ToolStripMenuItem Item(string text, EventHandler handler, bool danger)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Font = Fonts.Body;
            item.ForeColor = danger ? Pal.Danger : Pal.TextSoft;
            item.Padding = new Padding(Ui.D(8), Ui.D(4), Ui.D(16), Ui.D(4));
            item.Tag = danger ? "danger" : "normal";
            item.Click += handler;
            return item;
        }

        private ToolStripMenuItem DisabledItem(string text)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Enabled = false;
            item.Font = Fonts.Small;
            item.ForeColor = Pal.Muted;
            item.Padding = new Padding(Ui.D(8), Ui.D(2), Ui.D(12), Ui.D(2));
            return item;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
            {
                base.WndProc(ref m);
                if (m.Result == (IntPtr)1)
                {
                    long lp = m.LParam.ToInt64();
                    int sx = unchecked((short)(lp & 0xFFFF));
                    int sy = unchecked((short)((lp >> 16) & 0xFFFF));
                    Point p = PointToClient(new Point(sx, sy));
                    int b = Ui.D(ResizeBorder);
                    bool left = p.X < b;
                    bool right = p.X >= ClientSize.Width - b;
                    bool top = p.Y < b;
                    bool bottom = p.Y >= ClientSize.Height - b;
                    if (left && top) m.Result = (IntPtr)13;
                    else if (right && top) m.Result = (IntPtr)14;
                    else if (left && bottom) m.Result = (IntPtr)16;
                    else if (right && bottom) m.Result = (IntPtr)17;
                    else if (left) m.Result = (IntPtr)10;
                    else if (right) m.Result = (IntPtr)11;
                    else if (top) m.Result = (IntPtr)12;
                    else if (bottom) m.Result = (IntPtr)15;
                }
                return;
            }
            base.WndProc(ref m);
        }
    }

    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColors())
        {
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(Pal.PanelAlt);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen p = new Pen(Pal.Border))
                e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected) return;
            Rectangle r = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
            Gfx.FillRound(e.Graphics, r, 5f, Pal.PanelHi);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled) e.TextColor = Pal.Muted;
            else if (e.Item.Selected) e.TextColor = Pal.Text;
            else if (e.Item.Tag != null && e.Item.Tag.ToString() == "danger") e.TextColor = Pal.Danger;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (Pen p = new Pen(Pal.BorderSoft))
                e.Graphics.DrawLine(p, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2);
        }
    }

    internal class DarkMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Pal.PanelAlt; } }
        public override Color MenuBorder { get { return Pal.Border; } }
        public override Color MenuItemBorder { get { return Pal.PanelHi; } }
        public override Color MenuItemSelected { get { return Pal.PanelHi; } }
        public override Color ImageMarginGradientBegin { get { return Pal.PanelAlt; } }
        public override Color ImageMarginGradientMiddle { get { return Pal.PanelAlt; } }
        public override Color ImageMarginGradientEnd { get { return Pal.PanelAlt; } }
    }
}




