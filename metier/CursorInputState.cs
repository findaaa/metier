#nullable disable
using System;
using System.Windows.Forms;

namespace eep.editer1
{
    public class CursorInputState
    {
        private long lastInputTime = 0;
        public Keys LastKeyDown { get; private set; } = Keys.None;

        public bool IsImeComposing(IntPtr hWnd)
        {
            IntPtr hIMC = NativeMethods.ImmGetContext(hWnd);
            if (hIMC == IntPtr.Zero) return false;

            try
            {
                int strLen = NativeMethods.ImmGetCompositionString(hIMC, NativeMethods.GCS_COMPSTR, null, 0);
                return (strLen > 0);
            }
            finally
            {
                NativeMethods.ImmReleaseContext(hWnd, hIMC);
            }
        }

        public void RegisterKeyDown(Keys key)
        {
            LastKeyDown = key;
        }

        public void RegisterInput()
        {
            lastInputTime = DateTime.Now.Ticks / 10000;
        }

        public long GetMillisecondsSinceLastInput()
        {
            long now = DateTime.Now.Ticks / 10000;
            return now - lastInputTime;
        }

        public bool IsDeleting()
        {
            return (LastKeyDown == Keys.Back || LastKeyDown == Keys.Left);
        }
    }
}