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
        private long _lastSpaceReleaseTime = 0;

        private const int DOUBLE_TAP_SPEED = 600;
        private readonly List<(Color Color, string[] Keywords)> _colorDefinitions;

        private const string FONT_FAMILY = "Meiryo UI";
        private const float FONT_SIZE_NORMAL = 14;
        private const float FONT_SIZE_HEADING = 24;

        public TextStyler(RichTextBox richTextBox)
        {
            _richTextBox = richTextBox;
            _colorDefinitions = new List<(Color, string[])>
            {
                (Color.Red, new[] { "red", "赤色", "あか", "赤" }),
                (Color.Green, new[] { "green", "緑色", "みどり", "緑" })
            };
        }

        // ★修正: 戻り値を int (適用された高さ) に変更。何もしなければ 0 を返す。
        public int HandleShiftKeyUp()
        {
            long now = DateTime.Now.Ticks / 10000;
            int appliedHeight = 0;

            if (now - _lastShiftReleaseTime < DOUBLE_TAP_SPEED)
            {
                appliedHeight = ApplyHeadingLogic(); // 高さを受け取る
                _lastShiftReleaseTime = 0;
            }
            else _lastShiftReleaseTime = now;

            return appliedHeight;
        }

        // ★修正: 戻り値を int に変更
        private int ApplyHeadingLogic()
        {
            int caretPos = _richTextBox.SelectionStart;
            bool isAfterCharacter = caretPos > 0 && !char.IsWhiteSpace(_richTextBox.Text[caretPos - 1]);
            int resultHeight = 0;

            if (isAfterCharacter)
            {
                // 直前の塊を見出しにする
                int startPos = GetChunkStartPosition(caretPos);
                int length = caretPos - startPos;

                _richTextBox.Select(startPos, length);

                // フォント切り替え＆高さ取得
                Font newFont = ToggleCurrentSelectionFont();

                // 見出し化（大きい文字）になった場合、その高さを記録
                if (newFont.Size >= 20)
                {
                    resultHeight = newFont.Height;
                }

                // 選択解除して末尾へ。入力用フォントは標準に戻す
                _richTextBox.Select(caretPos, 0);
                _richTextBox.SelectionFont = new Font(FONT_FAMILY, FONT_SIZE_NORMAL, FontStyle.Regular);
            }
            else
            {
                // これから書く文字のサイズを切り替える
                Font newFont = ToggleCurrentSelectionFont();
                // 切り替え後のフォント高さを返す
                resultHeight = newFont.Height;
            }

            _richTextBox.Focus();
            return resultHeight;
        }

        // ★修正: 変更後のフォントを返すように変更
        private Font ToggleCurrentSelectionFont()
        {
            Font currentFont = _richTextBox.SelectionFont;
            bool isHeading = (currentFont != null && currentFont.Size >= 20);

            Font newFont;
            if (isHeading)
                newFont = new Font(FONT_FAMILY, FONT_SIZE_NORMAL, FontStyle.Regular);
            else
                newFont = new Font(FONT_FAMILY, FONT_SIZE_HEADING, FontStyle.Bold);

            _richTextBox.SelectionFont = newFont;
            return newFont;
        }

        public void HandleSpaceKeyUp()
        {
            long now = DateTime.Now.Ticks / 10000;
            if (now - _lastSpaceReleaseTime < DOUBLE_TAP_SPEED)
            {
                int currentPos = _richTextBox.SelectionStart;
                if (currentPos >= 2)
                {
                    _richTextBox.Select(currentPos - 2, 2);
                    ResetColorToBlack();
                    ResetToNormalFont();
                    _richTextBox.SelectedText = " ";
                    ResetColorToBlack();
                    ResetToNormalFont();
                }
                _lastSpaceReleaseTime = 0;
            }
            else _lastSpaceReleaseTime = now;
        }

        private int GetChunkStartPosition(int caretPos)
        {
            int startPos = caretPos;
            for (int i = caretPos - 1; i >= 0; i--)
            {
                if (char.IsWhiteSpace(_richTextBox.Text[i]))
                {
                    startPos = i + 1;
                    break;
                }
                if (i == 0) startPos = 0;
            }
            return startPos;
        }

        public bool ToggleColor(bool keepTriggerWord)
        {
            int caretPos = _richTextBox.SelectionStart;
            int startPos = GetChunkStartPosition(caretPos);

            if (startPos >= caretPos) return false;
            string chunkText = _richTextBox.Text.Substring(startPos, caretPos - startPos);

            var definition = _colorDefinitions.FirstOrDefault(d => d.Keywords.Any(k => chunkText.EndsWith(k)));

            if (definition.Keywords != null)
            {
                string foundKeyword = definition.Keywords.First(k => chunkText.EndsWith(k));
                Color targetColor = definition.Color;
                int modifyLength = chunkText.Length;

                if (!keepTriggerWord)
                {
                    _richTextBox.Select(startPos + modifyLength - foundKeyword.Length, foundKeyword.Length);
                    _richTextBox.SelectedText = "";
                    modifyLength -= foundKeyword.Length;
                }

                if (modifyLength > 0)
                {
                    _richTextBox.Select(startPos, modifyLength);
                    bool isAlreadyTargetColor = (_richTextBox.SelectionColor.ToArgb() == targetColor.ToArgb());
                    _richTextBox.SelectionColor = isAlreadyTargetColor ? Color.Black : targetColor;
                }

                _richTextBox.Select(startPos + modifyLength, 0);
                _richTextBox.SelectionColor = Color.Black;
                _richTextBox.Focus();
                return true;
            }
            return false;
        }

        public void ResetToNormalFont()
        {
            Font currentFont = _richTextBox.SelectionFont;
            if (currentFont != null && currentFont.Size >= 20)
            {
                _richTextBox.SelectionFont = new Font(FONT_FAMILY, FONT_SIZE_NORMAL, FontStyle.Regular);
            }
        }

        public void ResetColorToBlack()
        {
            _richTextBox.SelectionColor = Color.Black;
        }
    }
}