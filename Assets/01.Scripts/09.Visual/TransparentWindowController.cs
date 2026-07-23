// System
using System;
using System.Runtime.InteropServices;

// Unity
using UnityEngine;

namespace Minsung.Visual
{
    // 지정한 키 색상만 투명하게 바꿈
    public static class TransparentWindowController
    {
        /****************************************
        *                Fields
        ****************************************/

        private const string PREF_DESKTOP_REVEAL  = "Visual.DesktopReveal.Enabled";
        private const int    DESKTOP_REVEAL_DEFAULT = 1; // 켜기 = 1, 끄기 = 0

        private static bool _active;

        private static FullScreenMode _savedFullScreenMode;
        private static int  _savedWidth;
        private static int  _savedHeight;
        private static bool _windowModeSaved;


        // WINAPI 활용

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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

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

        private const uint SWP_NOZORDER     = 0x0004;
        private const uint SWP_NOACTIVATE   = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_SHOWWINDOW   = 0x0040;

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

        // Unity 창 핸들 - 보통 게임 창이 활성/포그라운드라 둘 중 하나로 잡힌다
        private static IntPtr ResolveUnityWindow()
        {
            IntPtr handle = GetActiveWindow();
            if (handle == IntPtr.Zero)
            {
                handle = GetForegroundWindow();
            }
            return handle;
        }

        // Win32 COLORREF는 0x00BBGGRR 순서 - Color32(RGBA)와 바이트 순서가 다르다
        private static uint ToColorRef(Color32 color)
        {
            return (uint)(color.r | (color.g << 8) | (color.b << 16));
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

        /// <summary> 사용자 설정 토글 - 기본 OFF. 켜져 있으면 바탕화면/알림/다른 창이 화면과 방송·녹화에 노출될 수 있다 </summary>
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

        /// <summary> 테두리 없는 풀스크린 으로 수정 </summary>
        public static void EnterBorderlessForReveal()
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
            Screen.SetResolution(current.width, current.height, FullScreenMode.FullScreenWindow);
        }

        /// <summary> 연출 전 화면 모드/해상도로 되돌린다 </summary>
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
                _windowHandle = ResolveUnityWindow();
                if (_windowHandle == IntPtr.Zero)
                {
                    return false;
                }

                // 원래 창 상태를 먼저 저장한다 - 패턴 종료/사망/씬 전환/예외 어디서든 이 값으로 되돌린다
                _originalStyle   = GetWindowLongAuto(_windowHandle, GWL_STYLE);
                _originalExStyle = GetWindowLongAuto(_windowHandle, GWL_EXSTYLE);
                _styleSaved      = true;

                // WS_EX_TRANSPARENT는 마우스 입력을 통째로 아래 데스크톱으로 넘기므로 절대 추가하지 않는다
                long exStyle = _originalExStyle.ToInt64() | WS_EX_LAYERED;
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
            RestoreWindowMode();
        }
    }
}
