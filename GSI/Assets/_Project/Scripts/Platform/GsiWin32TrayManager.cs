using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Windows 전용 네이티브 시스템 트레이 매니저.
/// 게임 창을 백그라운드로 완전히 숨기고(SW_HIDE), 트레이 영역에 아이콘을 주입하며,
/// 더블 클릭 시 다시 게임 창을 복구(SW_RESTORE)하는 초절전 최적화 기능을 관리합니다.
/// 에디터 환경과 Windows 외 플랫폼에서는 가볍게 로그/시뮬레이션으로 동작하여 안정성을 유지합니다.
/// </summary>
public sealed class GsiWin32TrayManager : MonoBehaviour
{
    public static GsiWin32TrayManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private string trayTooltip = "G.S.I Star System";

    private const int WM_USER = 0x0400;
    private const int WM_TRAYCALLBACK = WM_USER + 102;

    private const int NIM_ADD = 0x00000000;
    private const int NIM_DELETE = 0x00000002;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;

    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONUP = 0x0205;

    private const int SW_HIDE = 0;
    private const int SW_RESTORE = 9;
    private const int GWLP_WNDPROC = -4;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIconW(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIconW(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadIconW(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
        {
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        }
        return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    private IntPtr _unityHWnd = IntPtr.Zero;
    private IntPtr _prevWndProc = IntPtr.Zero;
    private WndProcDelegate _wndProcDelegate;
    private bool _isMinimizedToTray = false;
    private bool _restoreRequested = false;
    private int _savedTargetFrameRate = 60;
    private IntPtr _hIcon = IntPtr.Zero;

    private bool IsSupported => 
        !Application.isEditor && 
        Application.platform == RuntimePlatform.WindowsPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (IsSupported)
        {
            InitializeNativeHandleAndIcon();
        }
    }

    private void InitializeNativeHandleAndIcon()
    {
        try
        {
            _unityHWnd = GetActiveWindow();
            if (_unityHWnd == IntPtr.Zero)
            {
                return;
            }

            // 게임의 실행 파일(.exe)에서 0번째 인덱스의 리포지토리 로고 아이콘을 추출
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            _hIcon = ExtractIconW(IntPtr.Zero, exePath, 0);

            // 아이콘 추출 실패 시 Windows 기본 애플리케이션 아이콘으로 안전 폴백
            if (_hIcon == IntPtr.Zero)
            {
                _hIcon = LoadIconW(IntPtr.Zero, new IntPtr(32512));
            }

            // Unity 메인 윈도우 프로시저(WndProc) 후킹 등록 (안정적 GC 보호를 위해 델리게이트 참조 보존)
            _wndProcDelegate = CustomWndProc;
            IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
            _prevWndProc = SetWindowLongPtr(_unityHWnd, GWLP_WNDPROC, newWndProcPtr);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GsiWin32TrayManager] Native initialization failed: {e.Message}");
        }
    }

    private void Update()
    {
        // 백그라운드 스레드 메시지 또는 WndProc 콜백에서 메인 스레드 복구 요청이 들어왔는지 확인
        if (_restoreRequested)
        {
            _restoreRequested = false;
            RestoreFromTray();
        }
    }

    /// <summary>
    /// 게임 창을 완전히 숨기고 시스템 트레이에 등록합니다. 
    /// CPU/GPU 발열 방지를 위해 초절전 모드(1fps)로 강제 조정합니다.
    /// </summary>
    public void MinimizeToTray()
    {
        if (_isMinimizedToTray)
        {
            return;
        }

        _isMinimizedToTray = true;

        // 원래 프레임 레이트 백업 및 1fps 최적화 모드로 전환
        _savedTargetFrameRate = Application.targetFrameRate;
        Application.targetFrameRate = 1;

        if (IsSupported && _unityHWnd != IntPtr.Zero)
        {
            RegisterTrayIcon();
            ShowWindow(_unityHWnd, SW_HIDE);
        }
        else
        {
            // 에디터/타 플랫폼 테스트용 시뮬레이션: 비활성화 상태만 로깅
            Debug.Log("[GsiWin32TrayManager] Minimize to Tray Simulating (Target FrameRate set to 1fps). Double-click tray or click any keys to restore.");
            
            // 에디터에서는 2초 후 자동 복구 코루틴 작동 (테스트 편의용)
            StartCoroutine(EditorRestoreDelay());
        }
    }

    private System.Collections.IEnumerator EditorRestoreDelay()
    {
        yield return new WaitForSecondsRealtime(3f);
        _restoreRequested = true;
    }

    /// <summary>
    /// 시스템 트레이 아이콘을 파괴하고, 창을 원상복구(SW_RESTORE)하며, 원래의 프레임 레이트로 환원합니다.
    /// </summary>
    public void RestoreFromTray()
    {
        if (!_isMinimizedToTray)
        {
            return;
        }

        _isMinimizedToTray = false;

        // 프레임 레이트 복구
        Application.targetFrameRate = _savedTargetFrameRate > 0 ? _savedTargetFrameRate : 60;

        if (IsSupported && _unityHWnd != IntPtr.Zero)
        {
            UnregisterTrayIcon();
            ShowWindow(_unityHWnd, SW_RESTORE);
        }
        else
        {
            Debug.Log("[GsiWin32TrayManager] Restore From Tray Simulating (Target FrameRate restored).");
        }
    }

    private void RegisterTrayIcon()
    {
        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
            hWnd = _unityHWnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYCALLBACK,
            hIcon = _hIcon,
            szTip = trayTooltip
        };

        Shell_NotifyIconW(NIM_ADD, ref nid);
    }

    private void UnregisterTrayIcon()
    {
        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
            hWnd = _unityHWnd,
            uID = 1
        };

        Shell_NotifyIconW(NIM_DELETE, ref nid);
    }

    private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYCALLBACK)
        {
            int eventType = (int)lParam;
            // 더블 클릭(WM_LBUTTONDBLCLK) 혹은 마우스 왼쪽 클릭 업(WM_LBUTTONUP) 감지 시 메인 스레드에 복구 지시
            if (eventType == WM_LBUTTONDBLCLK || eventType == WM_LBUTTONUP)
            {
                _restoreRequested = true;
            }
        }

        if (_prevWndProc != IntPtr.Zero)
        {
            return CallWindowProc(_prevWndProc, hWnd, msg, wParam, lParam);
        }
        return IntPtr.Zero;
    }

    private void OnDisable()
    {
        CleanupNativeHook();
    }

    private void OnDestroy()
    {
        CleanupNativeHook();
    }

    private void CleanupNativeHook()
    {
        if (IsSupported && _unityHWnd != IntPtr.Zero)
        {
            UnregisterTrayIcon();

            // WndProc 원래대로 복구하여 메모리 누수 및 오동작 완벽 차단
            if (_prevWndProc != IntPtr.Zero)
            {
                SetWindowLongPtr(_unityHWnd, GWLP_WNDPROC, _prevWndProc);
                _prevWndProc = IntPtr.Zero;
            }
        }
        _wndProcDelegate = null;
    }
}
