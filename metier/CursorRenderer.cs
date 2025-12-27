#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace eep.editer1
{
    public class CursorRenderer
    {
        private readonly PictureBox _cursorBox;
        private Color _systemAccentColor = Color.DeepSkyBlue;

        private int _blinkTimer = 0;

        // ★変更: 更新頻度が上がったため、点滅間隔の数値を増やして調整
        // (10ms/6ms倍 ≒ 1.6倍遅く設定して、見た目の速度を維持)
        private const int BLINK_INTERVAL = 88;

        public CursorRenderer(PictureBox cursorBox)
        {
            _cursorBox = cursorBox;
            InitializeStyle();
            GetSystemAccentColor();
        }

        private void InitializeStyle()
        {
            if (_cursorBox != null)
            {
                _cursorBox.BackColor = Color.Black;
                _cursorBox.Width = 2;
                _cursorBox.Visible = true;
                _cursorBox.BringToFront();
            }
        }

        private void GetSystemAccentColor()
        {
            try
            {
                int colorRaw;
                bool opaque;
                NativeMethods.DwmGetColorizationColor(out colorRaw, out opaque);
                _systemAccentColor = Color.FromArgb(255, Color.FromArgb(colorRaw));
            }
            catch
            {
                _systemAccentColor = Color.DodgerBlue;
            }
        }

        public void ResetBlink()
        {
            _blinkTimer = 0;
        }

        public void Render(float x, float y, int height, bool isImeComposing, bool isTyping, Color currentColor)
        {
            if (_cursorBox == null) return;

            _cursorBox.Location = new Point((int)x, (int)y);
            _cursorBox.Height = height;

            if (isImeComposing)
            {
                bool isBlack = (currentColor.R == 0 && currentColor.G == 0 && currentColor.B == 0);
                _cursorBox.BackColor = isBlack ? _systemAccentColor : currentColor;
                _cursorBox.Width = 5;
                _cursorBox.Visible = true;
                _blinkTimer = 0;
            }
            else
            {
                _cursorBox.BackColor = currentColor;
                _cursorBox.Width = 2;

                if (isTyping)
                {
                    _cursorBox.Visible = true;
                    _blinkTimer = 0;
                }
                else
                {
                    _blinkTimer++;
                    bool isVisible = (_blinkTimer % (BLINK_INTERVAL * 2)) < BLINK_INTERVAL;
                    _cursorBox.Visible = isVisible;
                }
            }

            _cursorBox.BringToFront();
        }
    }
}