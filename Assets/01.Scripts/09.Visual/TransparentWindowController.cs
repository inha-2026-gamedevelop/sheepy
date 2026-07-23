// System
using System;
using System.Runtime.InteropServices;

// Unity
using UnityEngine;

namespace Minsung.Visual
{
    // Unity 최상위 창을 레이어드 윈도로 바꿔 '지정한 키 색상 픽셀만' 투명하게 만든다
    // 투명해진 영역 뒤로는 Windows(DWM)가 합성하는 실제 데스크톱이 그대로 보인다 - 화면을 읽거나 캡처하거나 저장하지 않는다
    // Windows 스탠드얼론 + 사용자 토글 ON에서만 동작하고, 그 외에는 아무 것도 하지 않고 false를 반환한다(폴백 배경은 호출부 책임)
    public static class TransparentWindowController
    {
        /****************************************
        *                Fields
        ****************************************/

        // 사용자 설정 - 공간찢기 연출에 실제 데스크톱 노출이 필수라 기본 ON이다(기획서 6장의 기본 OFF 방침에서 변경됨)
        // 설정에서 끌 수 있으며, 켜져 있는 동안 바탕화면/알림/다른 앱이 화면과 방송·녹화에 노출될 수 있음을 반드시 안내해야 한다
        private const string PREF_DESKTOP_REVEAL  = "Visual.DesktopReveal.Enabled";
        private const int    DESKTOP_REVEAL_DEFAULT = 1;

        private static bool _active;

        // 레이어드 창 투명은 DWM 합성이 필요해 전체화면(독립 플립)에서는 무시된다 - 연출 동안만 창 모드로 바꾼다
        private static FullScreenMode _savedFullScreenMode;
        private static int  _savedWidth;
        private static int  _savedHeight;
        private static bool _windowModeSaved;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int GWL_STYLE   = -16;
        private const int GWL_EXSTYLE = -20;

        private const uint WS_EX_LAYERED = 0x00080000;
        private const uint WS_BORDER     = 0x00800000;
        private const uint WS_CAPTION    = 0x00C00000;
        private const uint WS_THICKFRAME = 0x00040000;

        // 키 색상만 투명하게 만든다. LWA_ALPHA는 창 전체 불투명도를 바꿔 게임 조각과 보스까지 사라지므로 쓰지 않는다
        private const uint LWA_COLORKEY = 0x00000001;

        private static IntPtr _windowHandle = IntPtr.Zero;
        private static IntPtr _originalStyle;
        private static IntPtr _originalExStyle;
        private static bool   _styleSaved;

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        private const uint SWP_NOZORDER    = 0x0004;
        private const uint SWP_NOACTIVATE  = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_SHOWWINDOW  = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        private static RECT _originalRect;
        private static bool _rectSaved;

        // 32비트에서는 *Ptr 계열이 export되지 않아 일반 버전으로 내려간다
        private static IntPtr GetWindowLongAuto(IntPtr hWnd, int nIndex)
        {
            return (IntPtr.Size == 8) ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong(hWnd, nIndex));
        }

        private static IntPtr SetWindowLongAuto(IntPtr hWnd, int nIndex, IntPtr value)
        {
            return (IntPtr.Size == 8)
                ? SetWindowLongPtr64(hWnd, nIndex, value)
                : new IntPtr(SetWindowLong(hWnd, nIndex, value.ToInt32()));
        }
#endif

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 이 플랫폼에서 실제 데스크톱 노출이 가능한가 - Windows 스탠드얼론에서만 true </summary>
        public static bool IsSupported
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary> 사용자 설정 토글 - 기본 ON. 켜져 있으면 바탕화면/알림/다른 창이 화면과 방송·녹화에 노출될 수 있다 </summary>
        public static bool IsUserEnabled
        {
            get { return PlayerPrefs.GetInt(PREF_DESKTOP_REVEAL, DESKTOP_REVEAL_DEFAULT) != 0; }
            set
            {
                PlayerPrefs.SetInt(PREF_DESKTOP_REVEAL, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary> 지금 투명 창이 적용된 상태인가 </summary>
        public static bool IsActive => _active;

        /// <summary> 실제 데스크톱 노출을 쓸 수 있는 조건인가(플랫폼 + 사용자 토글) </summary>
        public static bool CanReveal => IsSupported && IsUserEnabled;

        /// <summary>
        /// 레이어드 투명이 동작할 수 있도록 전체화면을 창 모드로 바꾼다.
        /// Screen.SetResolution은 다음 프레임에 적용되므로, 호출부는 한 프레임 이상 기다린 뒤 TryEnable을 호출해야 한다.
        /// </summary>
        public static void EnterWindowedForReveal()
        {
            if (!CanReveal || _windowModeSaved)
            {
                return;
            }

            _savedFullScreenMode = Screen.fullScreenMode;
            _savedWidth          = Screen.width;
            _savedHeight         = Screen.height;
            _windowModeSaved     = true;

            Resolution current = Screen.currentResolution;
            Screen.SetResolution(current.width, current.height, FullScreenMode.Windowed);
        }

        /// <summary> 연출 전 화면 모드/해상도로 되돌린다(두 번 호출해도 안전) </summary>
        public static void RestoreWindowMode()
        {
            if (!_windowModeSaved)
            {
                return;
            }
            Screen.SetResolution(_savedWidth, _savedHeight, _savedFullScreenMode);
            _windowModeSaved = false;
        }

        /// <summary>
        /// 창을 레이어드 윈도로 바꾸고 keyColor와 정확히 같은 픽셀만 투명하게 만든다.
        /// 성공하면 true - 실패하거나 지원되지 않으면 false이며 호출부는 즉시 폴백 배경으로 되돌아가야 한다.
        /// </summary>
        public static bool TryEnable(Color32 keyColor, bool removeBorder)
        {
            if (_active)
            {
                return false;
            }
            if (!CanReveal)
            {
                Debug.Log($"[TransparentWindowController] 데스크톱 노출 건너뜀 - supported={IsSupported}, userEnabled={IsUserEnabled}");
                return false;
            }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                _windowHandle = GetActiveWindow();
                if (_windowHandle == IntPtr.Zero)
                {
                    return false;
                }

                // 원래 창 상태를 먼저 저장한다 - 패턴 종료/사망/씬 전환/예외 어디서든 이 값으로 되돌린다
                _originalStyle   = GetWindowLongAuto(_windowHandle, GWL_STYLE);
                _originalExStyle = GetWindowLongAuto(_windowHandle, GWL_EXSTYLE);
                _styleSaved      = true;

                long exStyle = _originalExStyle.ToInt64() | WS_EX_LAYERED;

                // WS_EX_TRANSPARENT는 마우스 입력을 통째로 아래 데스크톱으로 넘기므로 절대 추가하지 않는다
                SetWindowLongAuto(_windowHandle, GWL_EXSTYLE, new IntPtr(exStyle));

                if (removeBorder)
                {
                    long style = _originalStyle.ToInt64() & ~(WS_BORDER | WS_CAPTION | WS_THICKFRAME);
                    SetWindowLongAuto(_windowHandle, GWL_STYLE, new IntPtr(style));
                }

                uint colorRef = ToColorRef(keyColor);
                if (!SetLayeredWindowAttributes(_windowHandle, colorRef, 255, LWA_COLORKEY))
                {
                    Debug.LogWarning($"[TransparentWindowController] SetLayeredWindowAttributes 실패 - Win32 오류 {Marshal.GetLastWin32Error()}");
                    Restore();
                    return false;
                }

                // 창 모드로 바뀐 창을 모니터 전체에 맞춘다 - 전체화면처럼 보이면서도 DWM 합성은 유지된다
                if (GetWindowRect(_windowHandle, out _originalRect))
                {
                    _rectSaved = true;
                }
                Resolution current = Screen.currentResolution;
                SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0, current.width, current.height,
                    SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);

                _active = true;
                Debug.Log($"[TransparentWindowController] 데스크톱 노출 적용됨 - hwnd={_windowHandle}, key=#{keyColor.r:X2}{keyColor.g:X2}{keyColor.b:X2}, mode={Screen.fullScreenMode}, {current.width}x{current.height}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TransparentWindowController] 투명 창 전환 실패 - 폴백으로 되돌립니다: {e.Message}");
                Restore();
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary> 저장해둔 원래 창 스타일로 되돌린다(두 번 호출해도 안전) </summary>
        public static void Restore()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (!_styleSaved || (_windowHandle == IntPtr.Zero))
            {
                _active = false;
                return;
            }

            try
            {
                SetWindowLongAuto(_windowHandle, GWL_EXSTYLE, _originalExStyle);
                SetWindowLongAuto(_windowHandle, GWL_STYLE, _originalStyle);

                if (_rectSaved)
                {
                    SetWindowPos(_windowHandle, IntPtr.Zero, _originalRect.Left, _originalRect.Top,
                        _originalRect.Right - _originalRect.Left, _originalRect.Bottom - _originalRect.Top,
                        SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                    _rectSaved = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TransparentWindowController] 창 스타일 복원 실패: {e.Message}");
            }

            _styleSaved    = false;
            _windowHandle  = IntPtr.Zero;
#endif
            _active = false;
            RestoreWindowMode(); // 전체화면으로 되돌린다
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Win32 COLORREF는 0x00BBGGRR 순서 - Color32(RGBA)와 바이트 순서가 다르다
        private static uint ToColorRef(Color32 color)
        {
            return (uint)(color.r | (color.g << 8) | (color.b << 16));
        }
#endif
    }
}
