#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace eep.editer1
{
    public class TextStyler
    {
        private readonly RichTextBox _richTextBox;
        private long _lastShiftReleaseTime = 0;
        private const int SHIFT_DOUBLE_TAP_SPEED = 600;
        private readonly List<(Color Color, string[] Keywords)> _colorDefinitions;

        public TextStyler(RichTextBox richTextBox)
        {
            _richTextBox = richTextBox;
            _colorDefinitions = new List<(Color, string[])>
            {
                (Color.Red, new[] { "red", "赤色", "あか", "赤" }),
                (Color.Green, new[] { "green", "緑色", "みどり", "緑" })
            };
        }

        public void HandleShiftKeyUp()
        {
            long now = DateTime.Now.Ticks / 10000;
            if (now - _lastShiftReleaseTime < SHIFT_DOUBLE_TAP_SPEED)
            {
                ToggleHeading();
                _lastShiftReleaseTime = 0;
            }
            else _lastShiftReleaseTime = now;
        }

        private void ToggleHeading()
        {
            Font currentFont = _richTextBox.SelectionFont;
            bool isHeading = (currentFont != null && currentFont.Size >= 20);
            if (isHeading) _richTextBox.SelectionFont = new Font("Meiryo UI", 14, FontStyle.Regular);
            else _richTextBox.SelectionFont = new Font("Meiryo UI", 24, FontStyle.Bold);
            _richTextBox.Focus();
        }

        public bool ToggleColor(bool keepTriggerWord)
        {
            int caretPos = _richTextBox.SelectionStart;
            int lineIndex = _richTextBox.GetLineFromCharIndex(caretPos);
            int lineStart = _richTextBox.GetFirstCharIndexFromLine(lineIndex);

            int lineEnd = _richTextBox.Text.IndexOf('\n', lineStart);
            if (lineEnd == -1) lineEnd = _richTextBox.TextLength;
            int lineLength = lineEnd - lineStart;
            string lineText = _richTextBox.Text.Substring(lineStart, lineLength);

            var definition = _colorDefinitions.FirstOrDefault(d => d.Keywords.Any(k => lineText.EndsWith(k)));

            if (definition.Keywords != null)
            {
                Color targetColor = definition.Color;
                string foundKeyword = definition.Keywords.First(k => lineText.EndsWith(k));

                _richTextBox.Select(lineStart, lineLength);
                bool isAlreadyTargetColor = (_richTextBox.SelectionColor.ToArgb() == targetColor.ToArgb());

                if (isAlreadyTargetColor)
                {
                    _richTextBox.SelectionColor = Color.Black;
                }
                else
                {
                    if (!keepTriggerWord)
                    {
                        // キーワードを消去
                        _richTextBox.Select(lineStart + lineLength - foundKeyword.Length, foundKeyword.Length);
                        _richTextBox.SelectedText = "";
                        lineLength -= foundKeyword.Length;
                    }

                    // 消去後の行（空でも可）を選択して色を塗る
                    _richTextBox.Select(lineStart, lineLength);
                    _richTextBox.SelectionColor = targetColor;
                }

                // 選択を解除して末尾へ移動し、次の入力色を確定
                _richTextBox.Select(lineStart + lineLength, 0);
                _richTextBox.SelectionColor = isAlreadyTargetColor ? Color.Black : targetColor;

                _richTextBox.Focus();
                return true;
            }
            return false;
        }

        public void CheckEmptyLineAndReset()
        {
            // 全消去時のみ初期化。行頭の色変更を邪魔しない。
            if (_richTextBox.TextLength == 0)
            {
                _richTextBox.SelectionColor = Color.Black;
                _richTextBox.SelectionFont = new Font("Meiryo UI", 14, FontStyle.Regular);
            }
        }
    }
}