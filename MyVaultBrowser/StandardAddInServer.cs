using Inventor;
using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static MyVaultBrowser.Win32Api;

namespace MyVaultBrowser
{
    /// <summary>
    ///     This is the primary AddIn Server class that implements the ApplicationAddInServer interface
    ///     that all Inventor AddIns are required to implement. The communication between Inventor and
    ///     the AddIn is via the methods on this interface.
    /// </summary>
    [Guid("ffbbb57a-07f3-4d5c-97b0-e8e302247c7a")]
    public class StandardAddInServer : ApplicationAddInServer
    {
        private MultiUserModeEnum activeProjectType;
        private ApplicationEvents applicationEvents;
        private DockableWindowsEvents dockableWindowsEvents;
        private Application inventorApplication;
        private DockableWindow myVaultBrowser;
        private View activeView;

        // Dictionary to store the HWND of the view and the corresponding HWND of the vault browser.
        private Dictionary<int, IntPtr> hwndDic;

        private class Hook
        {
            private StandardAddInServer parent;
            private IntPtr hhook = IntPtr.Zero;
            private uint hview = 0;

            public Hook(StandardAddInServer addin, uint view)
            {
                parent = addin;
                hview = view;
            }

            public void SetEventHook(uint view, uint idProcess, uint idThread = 0)
            {
                // In case the previous hook is not unhooked.
                //if (hhook != IntPtr.Zero)
                //    UnHookEvent();

                hview = view;

                // Listen for object create in inventor process.
                hhook = SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, IntPtr.Zero,
                    procDelegate, idProcess, idThread, WINEVENT_OUTOFCONTEXT);
            }

            public void UnHookEvent(IntPtr hhook, int view)
            {
                UnhookWinEvent(hhook);

                //set it to Zero again, indicating there is no hook.
                hhook = IntPtr.Zero;
                view = 0;
            }

            private void WinEventProc(IntPtr hWinEventHook, uint eventType,
                IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                // filter out non-window objects... (eg. items within a listbox)
                if (idObject != 0 || idChild != 0)
                {
                    return;
                }

                // Find a window with class name #32770 (dialog), in which our vault browser lives.
                var stringBuilder = new StringBuilder(256);
                var ret = GetClassName(hwnd, stringBuilder, stringBuilder.Capacity);

                if (ret > 0 && stringBuilder.ToString() == DIALOG_CLASS_NAME)
                {
                    // Find the parent window and check the title of it,
                    // if it is Vault, then we are done.
                    var pHwnd = GetParent(hwnd);
                    ret = GetWindowText(pHwnd, stringBuilder, stringBuilder.Capacity);

                    if (ret > 0 && stringBuilder.ToString() == "Vault")
                    {
                        parent.AddHwnd((int)hview, pHwnd);
                        //UnHookEvent();
#if DEBUG
                        Debug.WriteLine($"Vault Browser: {(int)pHwnd:X}");
#endif
                    }
                }
            }
        }

        public void AddHwnd(int viewHwnd, IntPtr hwnd)
        {
            hwndDic[viewHwnd] = hwnd;
            if (myVaultBrowser.Visible)
                UpdateMyVaultBrowser();
        }

        private void UpdateMyVaultBrowser()
        {
            myVaultBrowser.Clear();
            if (hwndDic.ContainsKey(activeView.HWND))
            {
                activeView.Document.BrowserPanes["Vault"].Visible = false;
                myVaultBrowser.AddChild(hwndDic[activeView.HWND]);
            }
        }

        private void RestoreVaultBrowser()
        {
            myVaultBrowser.Clear();
            try
            {
                activeView.Document.BrowserPanes["Vault"].Visible = true;
            }
            catch
            {
                //If the document is closing, the vault browser may not be there anymore,
                //or if vault addin is not loaded?
            }
        }

        private void SubscribeEvents()
        {
            applicationEvents.OnActivateDocument += ApplicationEvents_OnActivateDocument;
            applicationEvents.OnDeactivateDocument += ApplicationEvents_OnDeactivateDocument;
            applicationEvents.OnNewView += ApplicationEvents_OnNewView;
            applicationEvents.OnCloseView += ApplicationEvents_OnCloseView;
            applicationEvents.OnActivateView += ApplicationEvents_OnActivateView;
            dockableWindowsEvents.OnShow += DockableWindowsEvents_OnShow;
            dockableWindowsEvents.OnHide += DockableWindowsEvents_OnHide;
        }

        private void UnSubscribeEvents()
        {
            applicationEvents.OnActivateDocument -= ApplicationEvents_OnActivateDocument;
            applicationEvents.OnDeactivateDocument -= ApplicationEvents_OnDeactivateDocument;
            applicationEvents.OnNewView -= ApplicationEvents_OnNewView;
            applicationEvents.OnCloseView -= ApplicationEvents_OnCloseView;
            applicationEvents.OnActivateView -= ApplicationEvents_OnActivateView;
            dockableWindowsEvents.OnShow -= DockableWindowsEvents_OnShow;
            dockableWindowsEvents.OnHide -= DockableWindowsEvents_OnHide;
        }

        private void ReloadVaultAddin()
        {
            try
            {
                ApplicationAddIn vaultAddin =
                    inventorApplication.ApplicationAddIns.ItemById["{48B682BC-42E6-4953-84C5-3D253B52E77B}"];
                if (vaultAddin.Activated)
                {
                    vaultAddin.Deactivate();
                }
                //Make sure vault addin is loaded and loaded after me.
                vaultAddin.Activate();
            }
            catch
            {
                //
            }
        }

        #region ApplicationAddInServer Members

        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            // This method is called by Inventor when it loads the addin.
            // The AddInSiteObject provides access to the Inventor Application object.
            // The FirstTime flag indicates if the addin is loaded for the first time.

            // Initialize AddIn members.
            inventorApplication = addInSiteObject.Application;

            applicationEvents = inventorApplication.ApplicationEvents;
            dockableWindowsEvents = inventorApplication.UserInterfaceManager.DockableWindows.Events;

            activeProjectType = inventorApplication.DesignProjectManager.ActiveDesignProject.ProjectType;
            if (activeProjectType == MultiUserModeEnum.kVaultMode)
            {
                SubscribeEvents();
                //if(inventorApplication.Ready)
                //    ReloadVaultAddin();
            }

            activeView = inventorApplication.ActiveView;

            hwndDic = new Dictionary<int, IntPtr>();

            applicationEvents.OnActiveProjectChanged += ApplicationEvents_OnActiveProjectChanged;

            if (myVaultBrowser == null)
            {
                myVaultBrowser =
                    inventorApplication.UserInterfaceManager.DockableWindows.Add(
                        "{ffbbb57a-07f3-4d5c-97b0-e8e302247c7a}",
                        "myvaultbrowser", "Vault");
                myVaultBrowser.ShowTitleBar = true;
                myVaultBrowser.DockingState = DockingStateEnum.kDockRight;
                myVaultBrowser.Visible = false;
            }
            else
            {
                myVaultBrowser =
                    inventorApplication.UserInterfaceManager.DockableWindows["myvaultbrowser"];
            }

#if DEBUG
            Win32Api.SetEventHook(0, (uint)Process.GetCurrentProcess().Id);
#endif
        }

        public void Deactivate()
        {
#if DEBUG
            Win32Api.UnHookEvent();
#endif
            // Release objects.
            if (activeProjectType == MultiUserModeEnum.kVaultMode)
                UnSubscribeEvents();
            applicationEvents.OnActiveProjectChanged -= ApplicationEvents_OnActiveProjectChanged;

            dockableWindowsEvents = null;
            applicationEvents = null;

            myVaultBrowser = null;
            activeView = null;

            hwndDic = null;

            Marshal.ReleaseComObject(inventorApplication);
            inventorApplication = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ExecuteCommand(int commandId)
        {
            // Note:this method is now obsolete, you should use the
            // ControlDefinition functionality for implementing commands.
        }

        public object Automation
        {
            // This property is provided to allow the AddIn to expose an API
            // of its own to other programs. Typically, this  would be done by
            // implementing the AddIn's API interface in a class and returning
            // that class object through this property.
            get { return null; }
        }

        #endregion ApplicationAddInServer Members

        #region Event Handlers

        private void ApplicationEvents_OnActiveProjectChanged(DesignProject ProjectObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            if (activeProjectType != ProjectObject.ProjectType)
            {
                if (ProjectObject.ProjectType == MultiUserModeEnum.kVaultMode)
                {
                    SubscribeEvents();
                    //ReloadVaultAddin();
                }
                else
                {
                    UnSubscribeEvents();
                }
                activeProjectType = ProjectObject.ProjectType;
            }
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void DockableWindowsEvents_OnHide(DockableWindow DockableWindow, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            if (BeforeOrAfter == EventTimingEnum.kBefore && DockableWindow.InternalName == "myvaultbrowser")
                RestoreVaultBrowser();
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void DockableWindowsEvents_OnShow(DockableWindow DockableWindow, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
#if DEBUG
            //Debug.WriteLine("DockableWindowsEvents_OnShow");
#endif
            if (BeforeOrAfter == EventTimingEnum.kBefore && DockableWindow.InternalName == "myvaultbrowser" &&
                hwndDic.ContainsKey(activeView.HWND))
                UpdateMyVaultBrowser();
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnCloseView(View ViewObject, EventTimingEnum BeforeOrAfter, NameValueMap Context,
            out HandlingCodeEnum HandlingCode)
        {
#if DEBUG
            //Debug.WriteLine("ApplicationEvents_OnCloseView");
            //Win32Api.UnHookEvent();
#endif
            if (BeforeOrAfter == EventTimingEnum.kBefore)
                hwndDic.Remove(ViewObject.HWND);
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnNewView(View ViewObject, EventTimingEnum BeforeOrAfter, NameValueMap Context,
            out HandlingCodeEnum HandlingCode)
        {
            if (BeforeOrAfter == EventTimingEnum.kAfter)
            {
                var doc = ViewObject.Document;
                if (doc.Views.Count > 1)
                    if (hwndDic.ContainsKey(doc.Views[1].HWND))
                        hwndDic[ViewObject.HWND] = hwndDic[doc.Views[1].HWND];
            }
#if DEBUG
            if (ViewObject != null)
                Debug.WriteLine($"OnNewView: {BeforeOrAfter}, Document: {ViewObject.Document.DisplayName}, " +
                                $"View: {ViewObject.HWND}, activeview: {activeView.HWND}, actualview:{inventorApplication.ActiveView.HWND}");
            else
                try
                {
                    Debug.WriteLine($"OnNewView: {BeforeOrAfter}, Document: {inventorApplication.ActiveDocument.DisplayName}, " +
                                    $"activeview: {activeView.HWND}, actualview:{inventorApplication.ActiveView.HWND}");
                }
                catch
                {
                    Debug.WriteLine($"OnNewView: {BeforeOrAfter}, Document: None, " +
                                    $"activeview: None, actualview: None");
                }
#endif
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnActivateView(View ViewObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            if (BeforeOrAfter == EventTimingEnum.kAfter)
            {
                activeView = ViewObject;
            }
#if DEBUG
            Debug.WriteLine($"OnActivateView: {BeforeOrAfter}, Document: {ViewObject.Document.DisplayName}, " +
                            $"View: {ViewObject.HWND}, activeview: {activeView.HWND}, actualview:{inventorApplication.ActiveView.HWND}");
#endif
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnDeactivateDocument(_Document DocumentObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
#if DEBUG
            Debug.WriteLine($"OnDeactivateDocument: {BeforeOrAfter}, Document: {DocumentObject.DisplayName}, " +
                            $"activeview: { activeView.HWND}, actualview: { inventorApplication.ActiveView.HWND}");
#endif
            if (BeforeOrAfter == EventTimingEnum.kBefore)
            {
                if (myVaultBrowser.Visible && DocumentObject.Equals(inventorApplication.ActiveDocument))
                    RestoreVaultBrowser();
            }
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnActivateDocument(_Document DocumentObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            if (BeforeOrAfter == EventTimingEnum.kAfter)
            {
                var viewHwnd = DocumentObject.Views[1].HWND;

                if (hwndDic.ContainsKey(viewHwnd))
                {
                    if (myVaultBrowser.Visible && DocumentObject.Equals(inventorApplication.ActiveDocument))
                        UpdateMyVaultBrowser();
                }
                //else
                //    Win32Api.SetEventHook((uint) viewHwnd, (uint) Process.GetCurrentProcess().Id);
                Hook test = new Hook(this);

            }
#if DEBUG
            Debug.WriteLine($"OnActivateDocument: {BeforeOrAfter}, Document: {DocumentObject.DisplayName}, " +
                            $"activeview: {activeView.HWND}, actualview: {inventorApplication.ActiveView.HWND}");
#endif
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        #endregion Event Handlers
    }
}
