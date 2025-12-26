#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace eep.editer1
{
    public partial class Metier : Form
    {
        private readonly CursorPhysics _physics;
        private readonly CursorRenderer _renderer;
        private readonly CursorInputState _inputState;
        private readonly TextStyler _textStyler;
        private readonly FileManager _fileManager;
        private readonly Stopwatch _stopwatch;

        private const float BASE_INTERVAL_MS = 10.0f;

        // 【修正】入力停止から物理演算がターゲットへ動き出すまでの猶予時間を Nms に短縮
        private const long PHYSICS_TIMEOUT_MS = 200;

        private const long BLINK_TIMEOUT_MS = 400;
        private const float MAX_ELAPSED_MS = 100.0f;
        private const float RATCHET_THRESHOLD_MULTIPLIER = 3.0f;
        private const int TIMER_INTERVAL_MS = 10;
        private const float Y_SNAP_THRESHOLD = 50.0f;

        private float _lastInputBaseLine = -1f;

        public Metier()
        {
            InitializeComponent();
            _stopwatch = new Stopwatch();
            _physics = new CursorPhysics();
            _inputState = new CursorInputState();
            _renderer = new CursorRenderer(cursorBox);
            _textStyler = new TextStyler(richTextBox1);
            _fileManager = new FileManager(richTextBox1);

            InitializeForm();
            InitializeRichTextBox();
            InitializeTimer();

            _fileManager.AutoLoad();
            _stopwatch.Start();
            timer1.Start();
        }

        private void InitializeForm()
        {
            Text = "metier";
            FormClosing += (s, e) => _fileManager.AutoSave();
        }

        private void InitializeRichTextBox()
        {
            richTextBox1.Text = "";
            richTextBox1.Font = new Font("Yu Gothic UI", 12, FontStyle.Regular);
            richTextBox1.ImeMode = ImeMode.On;
            richTextBox1.AcceptsTab = true;

            richTextBox1.SelectionChanged += (s, e) => { ForceHideSystemCaret(); _renderer.ResetBlink(); };
            richTextBox1.MouseDown += (s, e) => ForceHideSystemCaret();
            richTextBox1.GotFocus += (s, e) => ForceHideSystemCaret();

            richTextBox1.TextChanged += RichTextBox1_TextChanged;
            richTextBox1.KeyDown += RichTextBox1_KeyDown;
            richTextBox1.KeyUp += RichTextBox1_KeyUp;
        }

        private void InitializeTimer()
        {
            timer1.Interval = TIMER_INTERVAL_MS;
            timer1.Tick += Timer1_Tick;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            float deltaTime = CalculateDeltaTime();
            var metrics = GetCurrentCursorMetrics();
            var input = GetCurrentInputState();

            InitializeBaseLineIfNeeded(metrics);

            Point targetPosition = CalculateTargetPosition(metrics);

            // y軸（高さ）のずれは即時 snap 判定
            float diffY = Math.Abs(_physics.PosY - targetPosition.Y);
            bool isRowChanged = (diffY > Y_SNAP_THRESHOLD);

            // PHYSICS_TIMEOUT_MS を短くしたことで、x軸も 0.15秒 入力がないだけで
            // 物理演算が「収束フェーズ」に入り、正しい位置へ動き始めます。
            _physics.Update(
                targetPosition,
                input.IsTypingForPhysics || isRowChanged,
                input.IsDeleting,
                metrics.RatchetThreshold,
                deltaTime,
                metrics.CharWidth,
                input.IsComposing,
                input.ElapsedSinceInput
            );

            _renderer.Render(_physics.PosX, _physics.PosY - metrics.Height, metrics.Height, input.IsComposing, input.IsTypingForBlink, metrics.Color);

            ForceHideSystemCaret();
        }

        private void ForceHideSystemCaret()
        {
            NativeMethods.HideCaret(richTextBox1.Handle);
            NativeMethods.CreateCaret(richTextBox1.Handle, IntPtr.Zero, 0, 0);
        }

        private float CalculateDeltaTime()
        {
            float ms = (float)_stopwatch.Elapsed.TotalMilliseconds;
            _stopwatch.Restart();
            return (ms > MAX_ELAPSED_MS || ms <= 0f) ? 1.0f : ms / BASE_INTERVAL_MS;
        }

        private CursorMetrics GetCurrentCursorMetrics()
        {
            Point p = GetCaretPosition();
            Font f = richTextBox1.SelectionFont ?? richTextBox1.Font;
            return new CursorMetrics { RawPosition = p, Font = f, Height = f.Height, Color = richTextBox1.SelectionColor, CharWidth = TextRenderer.MeasureText("あ", f).Width, RatchetThreshold = f.Size * RATCHET_THRESHOLD_MULTIPLIER };
        }

        private InputStateInfo GetCurrentInputState()
        {
            long el = _inputState.GetMillisecondsSinceLastInput();
            bool comp = _inputState.IsImeComposing(richTextBox1.Handle);
            // ここで el < PHYSICS_TIMEOUT_MS (150ms) の判定が活きます
            return new InputStateInfo { IsComposing = comp, IsDeleting = _inputState.IsDeleting(), ElapsedSinceInput = el, IsTypingForPhysics = (el < PHYSICS_TIMEOUT_MS) || comp, IsTypingForBlink = el < BLINK_TIMEOUT_MS };
        }

        private void InitializeBaseLineIfNeeded(CursorMetrics m) { if (_lastInputBaseLine < 0) _lastInputBaseLine = m.RawPosition.Y + m.Height; }
        private Point CalculateTargetPosition(CursorMetrics m) => new Point(m.RawPosition.X, (int)_lastInputBaseLine);

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            _inputState.RegisterInput();
            ForceHideSystemCaret();
            _textStyler.CheckEmptyLineAndReset();

            if (!_inputState.IsImeComposing(richTextBox1.Handle))
            {
                Point p = GetCaretPosition();
                _lastInputBaseLine = p.Y + (richTextBox1.SelectionFont ?? richTextBox1.Font).Height;
            }
        }

        private void RichTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            _inputState.RegisterKeyDown(e.KeyCode);
            _renderer.ResetBlink();
            if (e.KeyCode == Keys.Tab && _textStyler.ToggleColor(e.Shift)) e.SuppressKeyPress = true;
        }

        private void RichTextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey) _textStyler.HandleShiftKeyUp();
            ForceHideSystemCaret();
        }

        private Point GetCaretPosition()
        {
            int idx = richTextBox1.SelectionStart;
            Point p = (idx < 0) ? new Point(0, 0) : richTextBox1.GetPositionFromCharIndex(idx);
            p.X += richTextBox1.Location.X; p.Y += richTextBox1.Location.Y;
            return p;
        }

        protected override void OnShown(EventArgs e) { base.OnShown(e); ForceHideSystemCaret(); }

        private struct CursorMetrics { public Point RawPosition; public Font Font; public int Height; public Color Color; public float CharWidth; public float RatchetThreshold; }
        private struct InputStateInfo { public bool IsComposing; public bool IsDeleting; public long ElapsedSinceInput; public bool IsTypingForPhysics; public bool IsTypingForBlink; }
    }
}