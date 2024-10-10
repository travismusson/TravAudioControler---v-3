//Pre - Alpha Version 3 - Actual Functional Version
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TravAudioControler___v_3
{
    public partial class Form1 : Form
    {
        //setting up constants for mouse and keyboard events
        const int VK_CONTROL = 0x11;
        const int WH_MOUSE_LL = 14;
        //importing windows API functions for capturing input
        [DllImport("user32.dll")]
        static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);
        //mouse hook callback function
        delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);     //utilizing a delegate
        static LowLevelMouseProc _proc = HookCallBack;
        static IntPtr _hookID = IntPtr.Zero;

        static MMDevice defaultDevice;      //naming defaultDevice of type MMDevice
        public Form1()
        {
            InitializeComponent();
            var deviceEnumerator = new MMDeviceEnumerator();        //new instance of enumerator
            defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);     //setting default device
            //defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = 0.05f;  //set volume to 50%
            _hookID = SetHook(_proc);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)       //after closing form release hooking
        {
            UnhookWindowsHookEx(_hookID);
            base.OnFormClosing(e);
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)       //method for setting the hook  - takes the delegate(proc) of type LowLevelMouseProc and returns the IntPtr which is a pointer to the hook
        {
            using (Process curProcess = Process.GetCurrentProcess())    // gets current process and wraps it with using statement which disposes after use (performance)
            using (ProcessModule curModule = curProcess.MainModule)     //gets main module of current process and wraps in using statement for dispose
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);           //calls the SetWindowsHookEx function to set the hook passing in the parameters of the mouse, process, and module handle(using windowsAPI), 0 indicates process is ascoiated with all threads
            }
        }

        private static IntPtr HookCallBack(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && GetKeyState(VK_CONTROL) < 0)
            {
                if (wParam == (IntPtr)0x020A)
                {
                    var mwStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    float currentVolume = defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                    int mouseDelta = (int)mwStruct.mouseData >> 16;     //extract the high-order word, which is the actual wheel data
                    //MessageBox.Show($"MouseData: {mwStruct.mouseData}\nMouseDelta: {mouseDelta}\nCurrent Volume: {currentVolume}");     //debugging
                    if (mouseDelta > 0)         //mouse up
                    {
                        currentVolume = Math.Min(currentVolume + 0.05f, 1.0f);  //ensures volume doesnt exceed 1.0
                        //defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar += 0.05f;
                        //MessageBox.Show($"Volume up: {currentVolume}");     //debugging
                    }
                    else if (mouseDelta < 0)       //mouse down
                    {
                        currentVolume = Math.Max(currentVolume - 0.05f, 0.0f);  //ensures volume doesnt go below
                        //defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar -= 0.05f;
                        //MessageBox.Show($"Volume down: {currentVolume}");
                    }
                    defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = currentVolume;
                    //MessageBox.Show($"Volume set to: {currentVolume}");
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private struct POINT
        {
            public int x;
            public int y;
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
