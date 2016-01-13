using Inventor;
using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static MyVaultBrowser.Win32Api;
// ReSharper disable InconsistentNaming

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
        private MultiUserModeEnum _activeProjectType;
        private ApplicationEvents _applicationEvents;
        private DockableWindowsEvents _dockableWindowsEvents;
        private Application _inventorApplication;
        private DockableWindow _myVaultBrowser;
        private View _activeView;

        // Dictionary to store the HWND of the view and the corresponding HWND of the vault browser.
        private Dictionary<int, IntPtr> _hwndDic;

        private static class Hook
        {
            private static StandardAddInServer _parent;
            private static IntPtr _hhook;
            private static Queue<int> _views;

            public static void Initialize(StandardAddInServer parent)
            {
                _parent = parent;
                _views = new Queue<int>();
            }

            public static void AddView(int hview)
            {
                _views.Enqueue(hview);
                Debug.WriteLine($"Number of views: {_views.Count}");
                if (_hhook == IntPtr.Zero)
                    SetEventHook();
            }

            private static void SetEventHook()
            {
                var idProcess = (uint) Process.GetCurrentProcess().Id;

                // Listen for object create in inventor process.
                _hhook = SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, IntPtr.Zero,
                    WinEventProc, idProcess, 0, WINEVENT_OUTOFCONTEXT);
            }

            private static void UnHookEvent()
            {
                UnhookWinEvent(_hhook);
                _hhook = IntPtr.Zero;
            }

            private static void WinEventProc(IntPtr hWinEventHook, uint eventType,
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
                        int hview;
                        _parent._hwndDic[hview = _views.Dequeue()] = pHwnd;//
                        if (_views.Count == 0)
                            UnHookEvent();
                        if (hview == _parent._activeView.HWND && _parent._myVaultBrowser.Visible)
                            _parent.UpdateMyVaultBrowser();
                        Debug.WriteLine($"Vault Browser: {(int)pHwnd:X}");
                    }
                }
            }
        }

        private void UpdateMyVaultBrowser()
        {
            //Debug.WriteLine($"hptr: {view:X}; HWND: {activeView.HWND:X}");

            //myVaultBrowser.Clear();
            if (_hwndDic.ContainsKey(_activeView.HWND))
            {
                _activeView.Document.BrowserPanes["Vault"].Visible = false;
                _myVaultBrowser.AddChild(_hwndDic[_activeView.HWND]);
            }
        }

        private void RestoreVaultBrowser()
        {
            _myVaultBrowser.Clear();
            try
            {
                if (_hwndDic.ContainsKey(_activeView.HWND))
                    _activeView.Document.BrowserPanes["Vault"].Visible = true;
            }
            catch
            {
                Debug.WriteLine("RestoreVaultBrowser");
                //TODO:
                //If the document is closing, the vault browser may not be there anymore,
                //or if vault addin is not loaded?
            }
        }

        private void SubscribeEvents()
        {
            _applicationEvents.OnActivateDocument += ApplicationEvents_OnActivateDocument;
            _applicationEvents.OnDeactivateDocument += ApplicationEvents_OnDeactivateDocument;
            _applicationEvents.OnNewView += ApplicationEvents_OnNewView;
            _applicationEvents.OnCloseView += ApplicationEvents_OnCloseView;
            _applicationEvents.OnActivateView += ApplicationEvents_OnActivateView;
            _dockableWindowsEvents.OnShow += DockableWindowsEvents_OnShow;
            _dockableWindowsEvents.OnHide += DockableWindowsEvents_OnHide;
        }

        private void UnSubscribeEvents()
        {
            _applicationEvents.OnActivateDocument -= ApplicationEvents_OnActivateDocument;
            _applicationEvents.OnDeactivateDocument -= ApplicationEvents_OnDeactivateDocument;
            _applicationEvents.OnNewView -= ApplicationEvents_OnNewView;
            _applicationEvents.OnCloseView -= ApplicationEvents_OnCloseView;
            _applicationEvents.OnActivateView -= ApplicationEvents_OnActivateView;
            _dockableWindowsEvents.OnShow -= DockableWindowsEvents_OnShow;
            _dockableWindowsEvents.OnHide -= DockableWindowsEvents_OnHide;
        }


        /// <summary>
        /// This method is used to make sure vault addin is loaded and after our addin, 
        /// so that the event handler in the vault addin is fired after our event handlers.
        /// </summary>
        private void ReloadVaultAddin()
        {
            try
            {
                ApplicationAddIn vaultAddin =
                    _inventorApplication.ApplicationAddIns.ItemById["{48B682BC-42E6-4953-84C5-3D253B52E77B}"];
                if (vaultAddin.Activated)
                {
                    vaultAddin.Deactivate();
                }
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
            _inventorApplication = addInSiteObject.Application;

            _applicationEvents = _inventorApplication.ApplicationEvents;
            _dockableWindowsEvents = _inventorApplication.UserInterfaceManager.DockableWindows.Events;

            _activeProjectType = _inventorApplication.DesignProjectManager.ActiveDesignProject.ProjectType;
            if (_activeProjectType == MultiUserModeEnum.kVaultMode)
            {
                SubscribeEvents();
                if (_inventorApplication.Ready)
                    ReloadVaultAddin();
            }

            _activeView = _inventorApplication.ActiveView;

            _hwndDic = new Dictionary<int, IntPtr>();
            Hook.Initialize(this);

            _applicationEvents.OnActiveProjectChanged += ApplicationEvents_OnActiveProjectChanged;

            if (_myVaultBrowser == null)
            {
                _myVaultBrowser =
                    _inventorApplication.UserInterfaceManager.DockableWindows.Add(
                        "{ffbbb57a-07f3-4d5c-97b0-e8e302247c7a}",
                        "myvaultbrowser", "Vault");
                _myVaultBrowser.ShowTitleBar = true;
                _myVaultBrowser.DockingState = DockingStateEnum.kDockRight;
                _myVaultBrowser.Visible = true;
            }
            else
            {
                _myVaultBrowser =
                    _inventorApplication.UserInterfaceManager.DockableWindows["myvaultbrowser"];
            }

        }

        public void Deactivate()
        {
            // Release objects.
            if (_activeProjectType == MultiUserModeEnum.kVaultMode)
                UnSubscribeEvents();
            _applicationEvents.OnActiveProjectChanged -= ApplicationEvents_OnActiveProjectChanged;

            _dockableWindowsEvents = null;
            _applicationEvents = null;

            _myVaultBrowser = null;
            _activeView = null;

            _hwndDic = null;

            Marshal.ReleaseComObject(_inventorApplication);
            _inventorApplication = null;

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
            //TODO: Before or After.
            if (_activeProjectType != ProjectObject.ProjectType)
            {
                if (ProjectObject.ProjectType == MultiUserModeEnum.kVaultMode)
                {
                    SubscribeEvents();
                    ReloadVaultAddin();
                }
                else
                {
                    UnSubscribeEvents();
                }
                _activeProjectType = ProjectObject.ProjectType;
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
            //Debug.WriteLine("DockableWindowsEvents_OnShow");
            if (BeforeOrAfter == EventTimingEnum.kBefore && DockableWindow.InternalName == "myvaultbrowser" &&
                _hwndDic.ContainsKey(_activeView.HWND))
                UpdateMyVaultBrowser();
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnCloseView(View ViewObject, EventTimingEnum BeforeOrAfter, NameValueMap Context,
            out HandlingCodeEnum HandlingCode)
        {
            //Debug.WriteLine("ApplicationEvents_OnCloseView");
            if (BeforeOrAfter == EventTimingEnum.kBefore)
                _hwndDic.Remove(ViewObject.HWND);
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnNewView(View ViewObject, EventTimingEnum BeforeOrAfter, NameValueMap Context,
            out HandlingCodeEnum HandlingCode)
        {
            if (BeforeOrAfter == EventTimingEnum.kAfter)
            {
                var doc = ViewObject.Document;
                if (doc.Views.Count > 1 && _hwndDic.ContainsKey(doc.Views[1].HWND))
                    _hwndDic[ViewObject.HWND] = _hwndDic[doc.Views[1].HWND];
            }
#if DEBUG
            if (ViewObject != null)
                Debug.WriteLine($"OnNewView: {BeforeOrAfter}, Document: {ViewObject.Document.DisplayName}, " +
                                $"View: {ViewObject.HWND}, activeview: {_activeView.HWND}, actualview:{_inventorApplication.ActiveView.HWND}");
            else
                try
                {
                    Debug.WriteLine($"OnNewView: {BeforeOrAfter}, Document: {_inventorApplication.ActiveDocument.DisplayName}, " +
                                    $"activeview: {_activeView.HWND}, actualview:{_inventorApplication.ActiveView.HWND}");
                }
                catch
                {
                    Debug.WriteLine($"OnNewView: {BeforeOrAfter}, Document: None, activeview: None, actualview: None");
                }
#endif
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnActivateView(View ViewObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            if (BeforeOrAfter == EventTimingEnum.kAfter)
            {
                _activeView = ViewObject;
            }
            Debug.WriteLine($"OnActivateView: {BeforeOrAfter}, Document: {ViewObject.Document.DisplayName}, " +
                            $"View: {ViewObject.HWND}, activeview: {_activeView.HWND}, actualview:{_inventorApplication.ActiveView.HWND}");
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnDeactivateDocument(_Document DocumentObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            Debug.WriteLine($"OnDeactivateDocument: {BeforeOrAfter}, Document: {DocumentObject.DisplayName}, " +
                            $"activeview: { _activeView.HWND}, actualview: { _inventorApplication.ActiveView.HWND}");
            if (BeforeOrAfter == EventTimingEnum.kBefore)
            {
                if (_myVaultBrowser.Visible && DocumentObject == _inventorApplication.ActiveDocument)
                    RestoreVaultBrowser();
            }
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnActivateDocument(_Document DocumentObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            if (BeforeOrAfter == EventTimingEnum.kAfter)
            {
                var hview = DocumentObject.Views[1].HWND;

                if (_hwndDic.ContainsKey(hview))
                {
                    if (_myVaultBrowser.Visible && DocumentObject == _inventorApplication.ActiveDocument)
                        UpdateMyVaultBrowser();
                }
                else
                {
                    Hook.AddView(hview);
                }
            }
            Debug.WriteLine($"OnActivateDocument: {BeforeOrAfter}, Document: {DocumentObject.DisplayName}, " +
                            $"activeview: {_activeView.HWND}, actualview: {_inventorApplication.ActiveView.HWND}");
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        #endregion Event Handlers
    }
}
