using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

        bool ctrl = false;


        public frmMain()
        {
            InitializeComponent();
            KBDHook.OnHookKeyPressEventHandler += new KBDHook.HookKeyPress(KBDHook_OnHookKeyPressEventHandler);
            listBox1.Items.Clear();
            KBDHook.LocalHook = true;
            KBDHook.InstallHook();
            label1.Text = string.Format("Installed:{0}\r\nModule:{1}\r\nLocal{2}",
                KBDHook.IsHookInstalled, KBDHook.ModuleHandle, KBDHook.LocalHook);
        }

        void KBDHook_OnHookKeyPressEventHandler(LLKHEventArgs e)
        {
            if (e.Keys == Keys.C && ctrl && e.IsPressed)
                listBox1.Items.Add("CONTROL + C pressed");

            if (e.Keys == Keys.ControlKey)
                ctrl = e.IsPressed;
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            KBDHook.UnInstallHook(); // Обязательно !!!
        }


namespace Hooks
{
    public static class KBDHook
    {
        #region Declarations
        public delegate void HookKeyPress(LLKHEventArgs e);
        public static event HookKeyPress OnHookKeyPressEventHandler;

        [StructLayout(LayoutKind.Sequential)]
        struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [Flags]
        enum KBDLLHOOKSTRUCTFlags : int
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80,
        }

        static IntPtr hHook = IntPtr.Zero;
        static IntPtr hModule = IntPtr.Zero;
        static bool hookInstall = false;
        static bool localHook = true;
        static API.HookProc hookDel;
        #endregion

        /// <summary>
        /// Hook install method.
        /// </summary>
        public static void InstallHook()
        {
            if (IsHookInstalled) return;

            hModule = Marshal.GetHINSTANCE(AppDomain.CurrentDomain.GetAssemblies()[0].GetModules()[0]);
            hookDel = new API.HookProc(HookProcFunction);

            if (localHook)
                hHook = API.SetWindowsHookEx(API.HookType.WH_KEYBOARD,
                    hookDel, IntPtr.Zero, AppDomain.GetCurrentThreadId());
            else
                hHook = API.SetWindowsHookEx(API.HookType.WH_KEYBOARD_LL,
                    hookDel, hModule, 0);

            if (hHook != IntPtr.Zero)
                hookInstall = true;
            else
                throw new Win32Exception("Can't install low level keyboard hook!");
        }
        /// <summary>
        /// If hook installed return true, either false.
        /// </summary>
        public static bool IsHookInstalled
        {
            get { return hookInstall && hHook != IntPtr.Zero; }
        }
        /// <summary>
        /// Module handle in which hook was installed.
        /// </summary>
        public static IntPtr ModuleHandle
        {
            get { return hModule; }
        }
        /// <summary>
        /// If true local hook will installed, either global.
        /// </summary>
        public static bool LocalHook
        {
            get { return localHook; }
            set
            {
                if (value != localHook)
                {
                    if (IsHookInstalled)
                        throw new Win32Exception("Can't change type of hook than it install!");
                    localHook = value;
                }
            }
        }
        /// <summary>
        /// Uninstall hook method.
        /// </summary>
        public static void UnInstallHook()
        {
            if (IsHookInstalled)
            {
                if (!API.UnhookWindowsHookEx(hHook))
                    throw new Win32Exception("Can't uninstall low level keyboard hook!");
                hHook = IntPtr.Zero;
                hModule = IntPtr.Zero;
                hookInstall = false;
            }
        }
        /// <summary>
        /// Hook process messages.
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        static IntPtr HookProcFunction(int nCode, IntPtr wParam, [In] IntPtr lParam)
        {
            if (nCode == 0)
            {
                if (localHook)
                {
                    bool pressed = false;
                    if (lParam.ToInt32() >> 31 == 0)
                        pressed = true;

                    Keys keys = (Keys)wParam.ToInt32();
                    if (OnHookKeyPressEventHandler != null)
                        OnHookKeyPressEventHandler(new LLKHEventArgs(keys, pressed, 0U, 0U));
                }
                else
                {
                    KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                    bool pressed = false;
                    if (wParam.ToInt32() == 0x100 || wParam.ToInt32() == 0x104)
                        pressed = true;

                    Keys keys = (Keys)kbd.vkCode;
                    if (OnHookKeyPressEventHandler != null)
                        OnHookKeyPressEventHandler(new LLKHEventArgs(keys, pressed, kbd.time, kbd.scanCode));
                }
            }

            return API.CallNextHookEx(hHook, nCode, wParam, lParam);
        }
    }

    public class LLKHEventArgs
    {
        Keys keys;
        bool pressed;
        uint time;
        uint scCode;

        public LLKHEventArgs(Keys keys, bool pressed, uint time, uint scanCode)
        {
            this.keys = keys;
            this.pressed = pressed;
            this.time = time;
            this.scCode = scanCode;
        }

        /// <summary>
        /// Key.
        /// </summary>
        public Keys Keys
        { get { return keys; } }
        /// <summary>
        /// Is key pressed or no.
        /// </summary>
        public bool IsPressed
        { get { return pressed; } }
        /// <summary>
        /// The time stamp for this message, equivalent to what GetMessageTime would return for this message.
        /// </summary>
        public uint Time
        { get { return time; } }
        /// <summary>
        /// A hardware scan code for the key.
        /// </summary>
        public uint ScanCode
        { get { return scCode; } }
    }

    public static class MouseHook
    {
        #region Declarations
        public static event MouseEventHandler MouseDown;
        public static event MouseEventHandler MouseUp;
        public static event MouseEventHandler MouseMove;

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEHOOKSTRUCT
        {
            public POINT pt;
            public IntPtr hwnd;
            public int wHitTestCode;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator Point(POINT p)
            {
                return new Point(p.X, p.Y);
            }

            public static implicit operator POINT(Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        const int WM_LBUTTONDOWN = 0x201;
        const int WM_LBUTTONUP = 0x202;
        const int WM_MOUSEMOVE = 0x0200;
        const int WM_MOUSEWHEEL = 0x020A;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_RBUTTONUP = 0x0205;
        const int WM_MBUTTONUP = 0x208;
        const int WM_MBUTTONDOWN = 0x207;
        const int WM_XBUTTONDOWN = 0x20B;
        const int WM_XBUTTONUP = 0x20C;

        static IntPtr hHook = IntPtr.Zero;
        static IntPtr hModule = IntPtr.Zero;
        static bool hookInstall = false;
        static bool localHook = true;
        static API.HookProc hookDel;
        #endregion

        /// <summary>
        /// Hook install method.
        /// </summary>
        public static void InstallHook()
        {
            if (IsHookInstalled) return;

            hModule = Marshal.GetHINSTANCE(AppDomain.CurrentDomain.GetAssemblies()[0].GetModules()[0]);
            hookDel = new API.HookProc(HookProcFunction);

            if (localHook)
                hHook = API.SetWindowsHookEx(API.HookType.WH_MOUSE,
                    hookDel, IntPtr.Zero, AppDomain.GetCurrentThreadId()); // Если подчеркивает необращай внимание, так надо.
            else
                hHook = API.SetWindowsHookEx(API.HookType.WH_MOUSE_LL,
                    hookDel, hModule, 0);

            if (hHook != IntPtr.Zero)
                hookInstall = true;
            else
                throw new Win32Exception("Can't install low level keyboard hook!");
        }
        /// <summary>
        /// If hook installed return true, either false.
        /// </summary>
        public static bool IsHookInstalled
        {
            get { return hookInstall && hHook != IntPtr.Zero; }
        }
        /// <summary>
        /// Module handle in which hook was installed.
        /// </summary>
        public static IntPtr ModuleHandle
        {
            get { return hModule; }
        }
        /// <summary>
        /// If true local hook will installed, either global.
        /// </summary>
        public static bool LocalHook
        {
            get { return localHook; }
            set
            {
                if (value != localHook)
                {
                    if (IsHookInstalled)
                        throw new Win32Exception("Can't change type of hook than it install!");
                    localHook = value;
                }
            }
        }
        /// <summary>
        /// Uninstall hook method.
        /// </summary>
        public static void UnInstallHook()
        {
            if (IsHookInstalled)
            {
                if (!API.UnhookWindowsHookEx(hHook))
                    throw new Win32Exception("Can't uninstall low level keyboard hook!");
                hHook = IntPtr.Zero;
                hModule = IntPtr.Zero;
                hookInstall = false;
            }
        }
        /// <summary>
        /// Hook process messages.
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        static IntPtr HookProcFunction(int nCode, IntPtr wParam, [In] IntPtr lParam)
        {
            if (nCode == 0)
            {
                if (localHook)
                {
                    MOUSEHOOKSTRUCT mhs = (MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MOUSEHOOKSTRUCT));
                    #region switch
                    switch (wParam.ToInt32())
                    {
                        case WM_LBUTTONDOWN:
                            if (MouseDown != null)
                                MouseDown(null,
                                    new MouseEventArgs(MouseButtons.Left,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_LBUTTONUP:
                            if (MouseUp != null)
                                MouseUp(null,
                                    new MouseEventArgs(MouseButtons.Left,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_MBUTTONDOWN:
                            if (MouseDown != null)
                                MouseDown(null,
                                    new MouseEventArgs(MouseButtons.Middle,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_MBUTTONUP:
                            if (MouseUp != null)
                                MouseUp(null,
                                    new MouseEventArgs(MouseButtons.Middle,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_MOUSEMOVE:
                            if (MouseMove != null)
                                MouseMove(null,
                                    new MouseEventArgs(MouseButtons.None,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_MOUSEWHEEL:
                            // Данный хук не позволяет узнать куда вращается колесо мыши.
                            break;
                        case WM_RBUTTONDOWN:
                            if (MouseDown != null)
                                MouseDown(null,
                                    new MouseEventArgs(MouseButtons.Right,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_RBUTTONUP:
                            if (MouseUp != null)
                                MouseUp(null,
                                    new MouseEventArgs(MouseButtons.Right,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        default:
                            //Debug.WriteLine(string.Format("X:{0}; Y:{1}; Handle:{2}; HitTest:{3}; EI:{4}; wParam:{5}; lParam:{6}",
                            //    mhs.pt.X, mhs.pt.Y, mhs.hwnd, mhs.wHitTestCode, mhs.dwExtraInfo, wParam.ToString(), lParam.ToString()));
                            break;
                    }
                    #endregion
                }
                else
                {
                    MSLLHOOKSTRUCT mhs = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    #region switch
                    switch (wParam.ToInt32())
                    {
                        case WM_LBUTTONDOWN:
                            if (MouseDown != null)
                                MouseDown(null,
                                    new MouseEventArgs(MouseButtons.Left,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_LBUTTONUP:
                            if (MouseUp != null)
                                MouseUp(null,
                                    new MouseEventArgs(MouseButtons.Left,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_MBUTTONDOWN:
                            if (MouseDown != null)
                                MouseDown(null,
                                    new MouseEventArgs(MouseButtons.Middle,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_MBUTTONUP:
                            if (MouseUp != null)
                                MouseUp(null,
                                    new MouseEventArgs(MouseButtons.Middle,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_MOUSEMOVE:
                            if (MouseMove != null)
                                MouseMove(null,
                                    new MouseEventArgs(MouseButtons.None,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_MOUSEWHEEL:
                            if (MouseMove != null)
                                MouseMove(null,
                                    new MouseEventArgs(MouseButtons.None, mhs.time,
                                        mhs.pt.X, mhs.pt.Y, mhs.mouseData >> 16));
                            //Debug.WriteLine(string.Format("X:{0}; Y:{1}; MD:{2}; Time:{3}; EI:{4}; wParam:{5}; lParam:{6}",
                            //            mhs.pt.X, mhs.pt.Y, mhs.mouseData, mhs.time, mhs.dwExtraInfo, wParam.ToString(), lParam.ToString()));
                            break;
                        case WM_RBUTTONDOWN:
                            if (MouseDown != null)
                                MouseDown(null,
                                    new MouseEventArgs(MouseButtons.Right,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        case WM_RBUTTONUP:
                            if (MouseUp != null)
                                MouseUp(null,
                                    new MouseEventArgs(MouseButtons.Right,
                                        1,
                                        mhs.pt.X,
                                        mhs.pt.Y,
                                        0));
                            break;
                        default:
                            
                            break;
                    }
                    #endregion
                }
            }

            return API.CallNextHookEx(hHook, nCode, wParam, lParam);
        }
    }

    static class API
    {
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, [In] IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, [In] IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn,
        IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public enum HookType : int
        {
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }
    }
}
