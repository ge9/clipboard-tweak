using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Threading.Timer;
using System.Text;
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

    [DllImport("user32.dll", SetLastError = true)]
    private extern static void AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    const int KEYEVENTF_KEYUP = 0x0002;

    int[] hotkey_ids = { 1, 2, 3 };

    //0: default status; do detecting
    //1: do nothing
    //2: after called SetText
    //3: after the first clipboard change in status 2
    //4: after Ctrl+V sent
    const int CBSTT_DEFAULT = 0;
    const int CBSTT_FINALIZE = 5;
    const int CBSTT_NOOP = 1;
    const int CBSTT_WAIT_COPY = 2;
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
        if (RegisterHotKey(this.Handle, hotkey_ids[0], MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.C) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }
        if (RegisterHotKey(this.Handle, hotkey_ids[1], MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.X) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }
        if (RegisterHotKey(this.Handle, hotkey_ids[2], MOD_ALT | MOD_SHIFT | MOD_CONTROL, (int)Keys.V) == 0)
        {
            MessageBox.Show("the hotkey is already in use");
        }
    }
    static private void backupCBto(ref System.Windows.Forms.IDataObject cb)
    {
        System.Windows.Forms.IDataObject cb_raw = null;
        while (true)
        {
            try
            {
                cb_raw = Clipboard.GetDataObject(); break;
            }
            catch (Exception)
            {
                Thread.Sleep(200);
            }
        }
        //last_cb = cb_raw; return;
        cb = new DataObject();
        foreach (string fmt in cb_raw.GetFormats(false))
        {
            if (!unsupported_formats.ContainsKey(fmt)) { Console.WriteLine(fmt); cb.SetData(fmt, cb_raw.GetData(fmt)); }
        }
        Console.WriteLine("stored!!");// + cb.GetData(DataFormats.UnicodeText));
    }
    static private void restoreCBfrom(System.Windows.Forms.IDataObject cb)
    {
        Clipboard.SetDataObject(cb, true);
        //foreach (string fmt in cb.GetFormats(false))
        //{
        //    Clipboard.SetData(fmt, cb.GetData(fmt));
        //}
        Console.WriteLine("restored!!!!!!!!!!!");
    }
    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
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
                if (clip_detect != CBSTT_DEFAULT) break;//does nothing in non-default state
                //release keys before sending Ctrl+C/Ctrl+V
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
                if (((int)message.WParam) == hotkey_ids[0] || ((int)message.WParam) == hotkey_ids[1])
                {
                    ShowWindow(this.Handle, SW_SHOWNA);
                    clip_detect = CBSTT_WAIT_COPY;
                    if (((int)message.WParam) == hotkey_ids[0]) { SendKeys.SendWait("^c"); } else { SendKeys.SendWait("^x"); }
                    SendKeys.Flush();
                }
                else if (((int)message.WParam) == hotkey_ids[2])
                {
                    clip_detect = CBSTT_CTRLV_RESTORE;
                    restoreCBfrom(spare_cbs[0]);
                }
                break;
            case WM_CLIPBOARDUPDATE:
                switch (clip_detect)
                {
                    case CBSTT_DEFAULT: clip_saved = false; break;
                    case CBSTT_WAIT_COPY:
                        backupCBto(ref spare_cbs[0]);
                        Console.WriteLine("copy done!");
                        goto case CBSTT_RESTORE_LAST;
                    case CBSTT_RESTORE_LAST:
                        clip_detect = CBSTT_FINALIZE;
                        Console.WriteLine("Restoring");
                        restoreCBfrom(last_cb);
                        Console.WriteLine("Restore Done");
                        break;
                    case CBSTT_CTRLV_RESTORE:
                        clip_detect = CBSTT_NOOP;
                        SendKeys.SendWait("^v");
                        SendKeys.Flush();
                        Timer timer = null;
                        timer = new Timer(_ => {
                            clip_detect = CBSTT_RESTORE_LAST;
                            PostMessage((int)myHwnd, WM_CLIPBOARDUPDATE, 0, 0);
                            timer.Dispose();
                        }, null, 100, Timeout.Infinite);//wait for 100ms before clipboard is used
                        break;
                    case CBSTT_FINALIZE:
                        ShowWindow(myHwnd, SW_HIDE);
                        clip_detect = CBSTT_DEFAULT;
                        break;
                }
                break;
        }
    }
}