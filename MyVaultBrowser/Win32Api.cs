using System;
using System.Runtime.InteropServices;
using System.Text;
// ReSharper disable InconsistentNaming

namespace MyVaultBrowser
{
    internal static class Win32Api
    {
        internal const uint EVENT_OBJECT_CREATE = 0x8000;

        internal const uint WINEVENT_OUTOFCONTEXT = 0;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
            hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
            uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
    }
}