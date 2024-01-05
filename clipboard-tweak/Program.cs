using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Threading.Timer;
class CB_Tweak_App : Form
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

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public const int KEYEVENTF_KEYUP = 0x0002;

    const int my_hotkey_id = 0x0010;//arbitrary value between 0x0000 and 0xbfff
    int[] hotkey_ids = { 1, 2, 3, 7, 4, 5, 6, 8 };
    const int my_hotkey_id_zot = 0x0012;//arbitrary value between 0x0000 and 0xbfff
    const int my_hotkey_id_code = 0x0013;//arbitrary value between 0x0000 and 0xbfff

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
    private System.Windows.Forms.IDataObject last_cb = new DataObject();
    private System.Windows.Forms.IDataObject[] spare_cbs = { new DataObject(), new DataObject() };
    private bool window_init_done = false;


    //use as multiset (bool is used like "unit" type)
    static Dictionary<string, bool> unsupported_formats = new Dictionary<string, bool>(){
        {"Object Descriptor", true},//Required in .NET Framework 4.x (tested 4.6.2)
        {"EnhancedMetafile", true},//Required in .NET Framework 3.x (tested 3.5) & 4.x (tested 4.6.2)
    };

    [STAThread]
    public static void Main(string[] args)
    {
        System.Windows.Forms.Application.Run(new CB_Tweak_App());
    }
    public CB_Tweak_App()
    {
        this.Visible = false;
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.TopMost = true;
        this.Size = new Size(800, 800);
        this.CenterToScreen();
        AddClipboardFormatListener(this.Handle);
        myHwnd = Handle;
        if (RegisterHotKey(this.Handle, my_hotkey_id, MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.Y) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }
        /*
        if (RegisterHotKey(this.Handle, hotkey_ids[4], MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.Z) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }*/
        if (RegisterHotKey(this.Handle, my_hotkey_id_zot, MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.P) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }
        if (RegisterHotKey(this.Handle, my_hotkey_id_code, MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.I) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }
        if (RegisterHotKey(this.Handle, hotkey_ids[1], MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.C) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }
        if (RegisterHotKey(this.Handle, hotkey_ids[2], MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.X) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }
        if (RegisterHotKey(this.Handle, hotkey_ids[3], MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.V) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }
    }
    static private void backupCBto(ref System.Windows.Forms.IDataObject cb)
    {
        System.Windows.Forms.IDataObject cb_raw = Clipboard.GetDataObject();
        //last_cb = cb_raw; return;
        cb = new DataObject();
        foreach (string fmt in cb_raw.GetFormats(false))
        {

            if (!unsupported_formats.ContainsKey(fmt)) { Console.WriteLine(fmt); cb.SetData(fmt, cb_raw.GetData(fmt)); }
        }
        Console.WriteLine("stored!!");// + cb.GetData(DataFormats.UnicodeText));
    }
    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        Timer timer = null;
        switch (message.Msg)
        {
            case WM_SHOWWINDOW:
                this.CenterToScreen();
                if ((int)message.WParam == 1)
                {
                    if (!window_init_done)
                    {//being shown
                        Console.WriteLine("Hiding");
                        window_init_done = true;
                        ShowWindow(this.Handle, SW_HIDE);
                    }
                }
                break;
            case WM_HOTKEY:
                //waitForKeysReleased();
                keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                //keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                keybd_event(0xA0, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(0xA1, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                //keybd_event(0xA2, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                //keybd_event(0xA3, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(0xA4, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(0xA5, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);


                //これがbackupより後だと、既にstoreしたものを復元後にもう一度storeすることになる可能性があるため、前にする
                clip_detect = CBSTT_NOOP;
                if (!clip_saved) { backupCBto(ref last_cb); Console.WriteLine("backup done!"); clip_saved = true; }
                //メイン部分
                if (((int)message.WParam) == hotkey_ids[1] || ((int)message.WParam) == hotkey_ids[2])
                {
                    ShowWindow(this.Handle, SW_SHOWNA);
                    clip_detect = CBSTT_NOOP;
                    if (((int)message.WParam) == hotkey_ids[1]) { SendKeys.SendWait("^c"); } else { SendKeys.SendWait("^x"); }
                    SendKeys.Flush();
                    Thread.Sleep(100);
                    backupCBto(ref spare_cbs[0]);
                    Console.WriteLine("copy done!");
                    timer = new Timer(_ => {
                        clip_detect = CBSTT_RESTORE_LAST;
                        PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                        timer.Dispose();
                    }, null, 100, Timeout.Infinite);
                }
                else if (((int)message.WParam) == hotkey_ids[3])
                {
                    clip_detect = CBSTT_WAIT_SET_AND_CTRLV;
                    Clipboard.SetDataObject(spare_cbs[0], true);
                }
                else if (((int)message.WParam) == my_hotkey_id)
                {
                    SendKeys.SendWait("^c");
                    SendKeys.Flush();
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
                else if (((int)message.WParam) == my_hotkey_id_zot)
                {
                    SendKeys.SendWait("^c");
                    SendKeys.Flush();
                    Thread.Sleep(50);
                    string text = Clipboard.GetText(TextDataFormat.UnicodeText);
                    if (text != null)
                    {
                        ProcessStartInfo pInfo = new ProcessStartInfo();
                        pInfo.FileName = "zot.bat";
                        pInfo.Arguments = text;
                        Process.Start(pInfo);
                    }
                    clip_detect = CBSTT_RESTORE_LAST;
                    PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                }
                else if (((int)message.WParam) == my_hotkey_id_code)
                {
                    SendKeys.SendWait("^c");
                    SendKeys.Flush();
                    Thread.Sleep(50);
                    string text = Clipboard.GetText(TextDataFormat.UnicodeText);
                    if (text != null)
                    {
                        SendText("`" + text);
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
                        timer = new Timer(_ => {
                            clip_detect = CBSTT_CTRLV_RESTORE;
                            PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                            timer.Dispose();
                        }, null, 100, Timeout.Infinite);
                        break;
                    case CBSTT_CTRLV_RESTORE:
                        clip_detect = CBSTT_NOOP;
                        SendKeys.SendWait("^v");
                        SendKeys.Flush();
                        timer = new Timer(_ => {
                            clip_detect = CBSTT_RESTORE_LAST;
                            PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                            timer.Dispose();
                        }, null, 100, Timeout.Infinite);
                        break;
                    case CBSTT_RESTORE_LAST:
                        clip_detect = CBSTT_FINALIZE;
                        Console.WriteLine("Restoring");
                        Clipboard.SetDataObject(last_cb, true);
                        Console.WriteLine("Restore Done");
                        break;
                    case CBSTT_FINALIZE:
                        clip_detect = CBSTT_NOOP;
                        timer = new Timer(_ => {
                            ShowWindow(myHwnd, SW_HIDE);
                            clip_detect = CBSTT_DEFAULT;
                            timer.Dispose();
                        }, null, 100, Timeout.Infinite);
                        break;
                }
                break;
        }
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


    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetMessageExtraInfo();
    static void SendText(string text)
    {
        foreach (char c in text)
        {
            var inputs = new INPUT[2];
            // Key Down
            inputs[0].type = 1; // Keyboard input
            inputs[0].u.ki.wScan = c;
            inputs[0].u.ki.dwFlags = 0x0004; // KEYEVENTF_UNICODE

            // Key Up
            inputs[1].type = 1; // Keyboard input
            inputs[1].u.ki.wScan = c;
            inputs[1].u.ki.dwFlags = 0x0004 | 0x0002; // KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
            Thread.Sleep(1);
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}