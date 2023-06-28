using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

using MessageBox = System.Windows.MessageBox;
using IDataObject = System.Windows.Forms.IDataObject;
using DataObject = System.Windows.Forms.DataObject;
using DataFormats = System.Windows.Forms.DataFormats;
using Clipboard = System.Windows.Forms.Clipboard;
using TextDataFormat = System.Windows.Forms.TextDataFormat;

class Q_col : System.Windows.Window
{
    const int MOD_ALT = 0x0001;
    const int MOD_CONTROL = 0x0002;
    const int MOD_SHIFT = 0x0004;

    const int SW_HIDE = 0x0000;
    const int SW_SHOWNA = 0x0004;

    const int WM_HOTKEY = 0x0312;

    const int WM_CLIPBOARDUPDATE = 0x31D;

    const int WM_SHOWWINDOW = 0x0018;

    [DllImport("User32.dll", EntryPoint = "PostMessage")]
    public static extern Int32 PostMessage(Int32 hWnd, Int32 Msg, Int32 wParam, Int32 lParam);

    [DllImport("user32.dll")]
    extern static int RegisterHotKey(IntPtr hWnd, int id, int modKey, int key);
    [DllImport("user32.dll")]
    extern static int UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll", SetLastError = true)]
    private extern static void AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    //[DllImport("user32.dll")]
    //public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);


    //public const int KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public INPUTUNION inputUnion;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public int uMsg;
        public short wParamL;
        public short wParamH;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    const uint INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_KEYUP = 0x0002;
    const ushort VK_SHIFT = 0x10;
    const ushort VK_CONTROL = 0x11;
    const ushort VK_MENU = 0x12;
    const ushort VK_C = 0x43;
    const ushort VK_P = 0x50;
    const ushort VK_V = 0x56;
    const ushort VK_X = 0x58;
    const ushort VK_Y = 0x59;
    const ushort VK_Z = 0x5A;
    const ushort VK_LSHIFT = 0xA0;
    const ushort VK_RSHIFT = 0xA1;
    const ushort VK_LCONTROL = 0xA2;
    const ushort VK_RCONTROL = 0xA3;
    const ushort VK_LMENU = 0xA4;
    const ushort VK_RMENU = 0xA5;



    const int my_hotkey_id = 0x0030;//arbitrary value between 0x0000 and 0xbfff
    (int, int, int, int)[] hotkey_ids = { (1, 2, 3, 7), (4, 5, 6, 8) };
    const int my_hotkey_id_zot = 0x0032;//arbitrary value between 0x0000 and 0xbfff

    //0: default status; do detecting
    //1: do nothing
    //2: after called SetText
    //3: after the first clipboard change in status 2
    //4: after Ctrl+V sent
    const int CBSTT_DEFAULT = 0;
    const int CBSTT_FINALIZE = 5;
    const int CBSTT_NOOP = 1;
    const int CBSTT_WAIT_SET_AND_CTRLV = 2;
    const int CBSTT_RESTORE_LAST = 4;
    const int CBSTT_CTRLV_RESTORE = 3;

    private int clip_detect = CBSTT_DEFAULT;
    private bool clip_saved = false;
    private IntPtr myHwnd = (IntPtr)0;
    private IDataObject last_cb = new DataObject();
    private IDataObject[] spare_cbs = { new DataObject(), new DataObject() };
    private bool window_init_done = false;

    
    //use as multiset (bool is used like "unit" type)
    static Dictionary<string, bool> unsupported_formats = new Dictionary<string, bool>(){
    {"Object Descriptor", true},
    };

    [STAThread]
    public static void Main(string[] args)
    {
        (new System.Windows.Application()).Run(new Q_col());
    }
    private void CenterToScreen()
    {
        double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
        double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
        double windowWidth = this.Width;
        double windowHeight = this.Height;
        this.Left = (screenWidth / 2) - (windowWidth / 2);
        this.Top = (screenHeight / 2) - (windowHeight / 2);
    }
    public Q_col()
    {
        var helper = new System.Windows.Interop.WindowInteropHelper(this);
        helper.EnsureHandle();
        myHwnd = helper.Handle;
        this.WindowStyle = System.Windows.WindowStyle.None; // 等価するものを選ぶ
        this.WindowState = System.Windows.WindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Topmost = true;
        this.Width = 800; // WPFではウィンドウのSizeはWidthとHeightで設定します
        this.Height = 800;
        this.CenterToScreen();

        this.Loaded += async (s, e) =>
        {
            await Task.Delay(100);
            this.Dispatcher.Invoke(() =>
            {
                ShowWindow(myHwnd, SW_HIDE);
            });
            var hwndSource = HwndSource.FromHwnd(myHwnd);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProcMy);
            }
        };
        //this.CenterWindow();
        //this.Visible = false;
        //this.WindowState = FormWindowState.Minimized;
        //this.ShowInTaskbar = false;
        //this.TopMost = true;
        //this.Size = new Size(800, 800);
        AddClipboardFormatListener(myHwnd);
        if (RegisterHotKey(myHwnd, my_hotkey_id, MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)VK_Y) == 0)
        {
            MessageBox.Show("the hotkey is already in use5");
        }
        /*
        if (RegisterHotKey(this.Handle, hotkey_ids[0].Item4, MOD_ALT | MOD_SHIFT | MOD_CONTROL, VK_Z) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }*/
        if (RegisterHotKey(myHwnd, my_hotkey_id_zot, MOD_ALT | MOD_SHIFT | MOD_CONTROL, VK_P) == 0)
        {
            MessageBox.Show("the hotkey is already in use4");
        }
        if (RegisterHotKey(myHwnd, hotkey_ids[0].Item1, MOD_ALT | MOD_SHIFT | MOD_CONTROL, VK_C) == 0)
        {
            MessageBox.Show("the hotkey is already in use3");
        }
        if (RegisterHotKey(myHwnd, hotkey_ids[0].Item2, MOD_ALT | MOD_SHIFT | MOD_CONTROL, VK_X) == 0)
        {
            MessageBox.Show("the hotkey is already in use2");
        }
        if (RegisterHotKey(myHwnd, hotkey_ids[0].Item3, MOD_ALT | MOD_SHIFT | MOD_CONTROL, VK_V) == 0)
        {
            MessageBox.Show("the hotkey is already in use1");
        }
    }
    static private void backupCBto(ref IDataObject cb)
    {
        IDataObject cb_raw = Clipboard.GetDataObject();
        //last_cb = cb_raw; return;
        cb = new DataObject();
        foreach (string fmt in cb_raw.GetFormats(false))
        {
            if (!unsupported_formats.ContainsKey(fmt)) { Console.WriteLine(fmt); cb.SetData(fmt, cb_raw.GetData(fmt)); }
        }
        Console.WriteLine("stored!!" + cb.GetData(DataFormats.UnicodeText));
    }
    /*
    private uint KeyPress(ushort key)
    {
        var keyDown = new INPUT { type = INPUT_KEYBOARD };
        keyDown.inputUnion.ki.wVk = key;
        return SendInput(1, ref keyDown, Marshal.SizeOf(typeof(INPUT)));
    }
    */
    private uint KeyRelease(ushort key)
    {
        var keyUp = new INPUT { type = INPUT_KEYBOARD };
        keyUp.inputUnion.ki.wVk = key;
        keyUp.inputUnion.ki.dwFlags = KEYEVENTF_KEYUP;
        return SendInput(1, ref keyUp, Marshal.SizeOf(typeof(INPUT)));
    }
    /*
    private void SimulateCtrlPlus(ushort MyKey)
    {
        Console.Write(Convert.ToString(Marshal.GetLastWin32Error()));
        Console.Write(Convert.ToString(Marshal.GetLastWin32Error()));
        Console.Write(Convert.ToString(Marshal.GetLastWin32Error()));
        Console.Write(Convert.ToString(Marshal.GetLastWin32Error()));

    }
    */

    private IntPtr WndProcMy(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        //base.WndProc(ref message);
        switch (msg)
        {
            case WM_SHOWWINDOW:
                this.CenterToScreen();
                if ((int)wParam == 1)
                {
                    if (!window_init_done)
                    {//being shown
                        Console.WriteLine("Hiding");
                        window_init_done = true;
                        ShowWindow(myHwnd, SW_HIDE);
                    }
                }
                break;
            case WM_HOTKEY:
                //waitForKeysReleased();
                KeyRelease(VK_SHIFT);
                //KeyRelease(VK_CONTROL);
                KeyRelease(VK_MENU);

                KeyRelease(VK_LSHIFT);
                KeyRelease(VK_RSHIFT);
                //KeyRelease(VK_LCONTROL);
                //KeyRelease(VK_RCONTROL);
                KeyRelease(VK_LMENU);
                KeyRelease(VK_RMENU);
                SendKeys.Flush();

                //これがbackupより後だと、既にstoreしたものを復元後にもう一度storeすることになる可能性があるため、前にする
                clip_detect = CBSTT_NOOP;
                if (!clip_saved) { backupCBto(ref last_cb); Console.WriteLine("backup done!"); clip_saved = true; }
                //メイン部分
                if (((int)wParam) == hotkey_ids[0].Item1 || ((int)wParam) == hotkey_ids[0].Item2)
                {
                    ShowWindow(myHwnd, SW_SHOWNA);//SW_SHOWNAはWPFにないのでShowWindow必須？
                    clip_detect = CBSTT_NOOP;
                    if (((int)wParam) == hotkey_ids[0].Item1) { SendKeys.SendWait("^c"); } else { SendKeys.SendWait("^x"); }
                    Thread.Sleep(100);
                    backupCBto(ref spare_cbs[0]);
                    Console.WriteLine("copy done!");
                    System.Threading.Tasks.Task.Delay(100).ContinueWith(
                     _ => {
                         clip_detect = CBSTT_RESTORE_LAST;
                         PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                     });
                }
                else if (((int)wParam) == hotkey_ids[0].Item3)
                {
                    clip_detect = CBSTT_WAIT_SET_AND_CTRLV;
                    Clipboard.SetDataObject(spare_cbs[0], true);
                }
                else if (((int)wParam) == my_hotkey_id)
                {
                    SendKeys.SendWait("^c");
                    Thread.Sleep(50);
                    string img = Clipboard.GetText(TextDataFormat.UnicodeText);
                    if (img != null)
                    {
                        //Console.WriteLine(img);
                        clip_detect = CBSTT_WAIT_SET_AND_CTRLV;
                        Clipboard.SetText(AddToUnicode(img), TextDataFormat.UnicodeText);
                    }
                    else
                    {
                        clip_detect = CBSTT_RESTORE_LAST;
                        PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                    }
                }
                else if (((int)wParam) == my_hotkey_id_zot)
                {
                    SendKeys.SendWait("^c");
                    Thread.Sleep(50);
                    string img = Clipboard.GetText(TextDataFormat.UnicodeText);
                    if (img != null)
                    {
                        ProcessStartInfo pInfo = new ProcessStartInfo();
                        pInfo.FileName = "zot.bat";
                        pInfo.Arguments = img;
                        Process.Start(pInfo);
                    }
                    clip_detect = CBSTT_RESTORE_LAST;
                    PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                }
                break;
            case WM_CLIPBOARDUPDATE:
                switch (clip_detect)
                {
                    case CBSTT_DEFAULT: clip_saved = false; break;
                    case CBSTT_WAIT_SET_AND_CTRLV:
                        clip_detect = CBSTT_NOOP;
                        System.Threading.Tasks.Task.Delay(100).ContinueWith(
                         _ => {
                             clip_detect = CBSTT_CTRLV_RESTORE;
                             PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                         });
                        break;
                    case CBSTT_CTRLV_RESTORE:
                        clip_detect = CBSTT_NOOP;
                        SendKeys.SendWait("^v");
                        System.Threading.Tasks.Task.Delay(100).ContinueWith(
                         _ => {
                             clip_detect = CBSTT_RESTORE_LAST;
                             PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                         });
                        break;
                    case CBSTT_RESTORE_LAST:
                        clip_detect = CBSTT_FINALIZE;
                        Console.WriteLine("Restoring");
                        Clipboard.SetDataObject(last_cb, true);
                        Console.WriteLine("Restore Done");
                        break;
                    case CBSTT_FINALIZE:
                        clip_detect = CBSTT_NOOP;
                        System.Threading.Tasks.Task.Delay(100).ContinueWith(
                         _ => {
                             ShowWindow(myHwnd, SW_HIDE);
                             clip_detect = CBSTT_DEFAULT;
                         });
                        break;
                }
                break;
        }
        return IntPtr.Zero;
    }
    static string AddToUnicode(string input)
    {
        List<char> res = new List<char>();
        char[] charArray = input.ToCharArray();
        bool contain_encoded()
        {
            for (int i = 0; i < charArray.Length; i++)
            {
                if ((0xd940 <= charArray[i] && charArray[i] < 0xdb40) || (0xdb80 <= charArray[i] && charArray[i] < 0xdc00)) return true;
            }
            return false;
        }
        if (charArray == null || charArray.Length == 0)
        {
            return "";
        }
        else if (!contain_encoded())//not converted yet
        {
            for (int i = 0; i < charArray.Length; i++)
            {
                if (char.IsSurrogate(charArray[i]))
                {
                    //add (0x60000 / 0x400)
                    //add 0x10000 for characters after U+E0000;
                    res.Add((char)(charArray[i] + (charArray[i] >= 0xdb40 ? 0x40 : 0x180)));
                    res.Add(charArray[++i]);
                }
                else//convert to surrogate pair
                {
                    int unicodePoint = (int)charArray[i];
                    res.Add((char)(unicodePoint / 0x400 + 0x140 + 0xd800));//0xd800 is at 0x10000, so we add (0x60000 - 0x10000) / 0x400
                    res.Add((char)(unicodePoint % 0x400 + 0xdc00));
                }
            }
            return new string(res.ToArray());
        }
        else//back
        {
            for (int i = 0; i < charArray.Length; i++)
            {
                if ((int)charArray[i] < 0xd800 || (int)charArray[i] > 0xe000)//not converted & not surrogate
                {
                    res.Add(charArray[i]); continue;
                }
                else if ((0xd800 <= (int)charArray[i] && (int)charArray[i] < 0xd940) || (0xdb40 <= (int)charArray[i] && (int)charArray[i] < 0xdb80))//not converted & surrogate
                {
                    res.Add(charArray[i]); res.Add(charArray[++i]); continue;
                }
                else if ((int)charArray[i] > 0xd980)//original char > 0x10000
                {
                    //subtract (0x60000 / 0x400)
                    //subtract 0x10000 for characters after U+F0000;
                    res.Add((char)(charArray[i] - (charArray[i] >= 0xdb80 ? 0x40 : 0x180)));
                    res.Add(charArray[++i]);
                }
                else//back to BMP
                {
                    int unicodePoint = (int)charArray[i];
                    res.Add((char)((charArray[i] - 0xd800 - 0x140) * 0x400 + (charArray[++i] - 0xdc00)));//To surrogate pair; 0xd800 is at 0x10000, so we add (0x60000 - 0x10000) / 0x400
                }
            }
            return new string(res.ToArray());
        }
    }
    static void waitForKeysReleased()
    {
        while (
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftAlt) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightAlt) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Y) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.P) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.C) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.X) ||
        //System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Z) ||
        System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.V)
            )
        {
            Thread.Sleep(100);
        }
        return;
    }
}