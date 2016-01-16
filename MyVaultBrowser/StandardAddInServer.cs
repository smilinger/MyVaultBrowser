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
        private Application _inventorApplication;
        private MultiUserModeEnum _activeProjectType;
        private ApplicationEvents _applicationEvents;
        private DockableWindowsEvents _dockableWindowsEvents;
        private DockableWindow _myVaultBrowser;

        // Dictionary to store the documents and the corresponding HWNDs of the vault browser.
        private Dictionary<Document, IntPtr> _hwndDic;

        private static class Hook
        {
            private static StandardAddInServer _parent;
            private static IntPtr _hhook;
            private static Queue<Document> _documents;

            // Need to ensure delegate is not garbage collected while we're using it,
            // storing it in a class field is simplest way to do this.
            private static readonly WinEventDelegate _procDelegate = WinEventProc;

            public static void Initialize(StandardAddInServer parent)
            {
                _parent = parent;
                _documents = new Queue<Document>();
            }

            public static void Clear()
            {
                if (_hhook != IntPtr.Zero)
                    UnHookEvent();
                _parent = null;
                _documents = null;
            }

            public static void AddDocument(Document doc)
            {
                _documents.Enqueue(doc);
                Debug.WriteLine($"Number of Documents: {_documents.Count}");
                if (_hhook == IntPtr.Zero)
                    SetEventHook();
            }

            private static void SetEventHook()
            {
                var idProcess = (uint) Process.GetCurrentProcess().Id;

                // Listen for object create in inventor process.
                _hhook = SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE, IntPtr.Zero,
                    _procDelegate, idProcess, 0, WINEVENT_OUTOFCONTEXT);
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
                        Document doc;
                        _parent._hwndDic[doc = _documents.Dequeue()] = pHwnd;//
                        if (_documents.Count == 0)
                            UnHookEvent();
                        if (doc == _parent._inventorApplication.ActiveDocument && _parent._myVaultBrowser.Visible)
                            _parent.UpdateMyVaultBrowser(doc);
                        Debug.WriteLine($"Vault Browser: {(int)pHwnd:X}");
                    }
                }
            }
        }

        private void UpdateMyVaultBrowser(Document doc)
        {
            doc.BrowserPanes["Vault"].Visible = false;
            _myVaultBrowser.AddChild(_hwndDic[doc]);
        }

        private void RestoreVaultBrowser(Document doc)
        {
            _myVaultBrowser.Clear();
            try
            {
                doc.BrowserPanes["Vault"].Visible = true;
            }
            catch
            {
                //If the document is closing, the vault browser may not be there anymore,
                //or if vault addin is not loaded?
            }
        }

        private void SubscribeEvents()
        {
            _applicationEvents.OnActivateDocument += ApplicationEvents_OnActivateDocument;
            _applicationEvents.OnDeactivateDocument += ApplicationEvents_OnDeactivateDocument;
            _applicationEvents.OnCloseView += ApplicationEvents_OnCloseView;
            _dockableWindowsEvents.OnShow += DockableWindowsEvents_OnShow;
            _dockableWindowsEvents.OnHide += DockableWindowsEvents_OnHide;
        }

        private void UnSubscribeEvents()
        {
            _applicationEvents.OnActivateDocument -= ApplicationEvents_OnActivateDocument;
            _applicationEvents.OnDeactivateDocument -= ApplicationEvents_OnDeactivateDocument;
            _applicationEvents.OnCloseView -= ApplicationEvents_OnCloseView;
            _dockableWindowsEvents.OnShow -= DockableWindowsEvents_OnShow;
            _dockableWindowsEvents.OnHide -= DockableWindowsEvents_OnHide;
        }


        /// <summary>
        /// This method is used to make sure vault addin is loaded and after our addin, 
        /// so that the event handler in the vault addin is fired after our event handlers.
        /// </summary>
        private void ReloadVaultAddin()
        {
            ApplicationAddIn vaultAddin =
                _inventorApplication.ApplicationAddIns.ItemById["{48B682BC-42E6-4953-84C5-3D253B52E77B}"];
            try
            {
                vaultAddin.Deactivate();
                if (_inventorApplication.ActiveDocument != null)
                    Hook.AddDocument(_inventorApplication.ActiveDocument);
                vaultAddin.Activate();
            }
            catch
            {
                //
            }
            if (!vaultAddin.Activated)
            {
                Debug.WriteLine("Cannot load vault addin, MyVaultBrowser will not work!");
                Deactivate();
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

            _hwndDic = new Dictionary<Document, IntPtr>();
            Hook.Initialize(this);

            _myVaultBrowser =
                _inventorApplication.UserInterfaceManager.DockableWindows.Add("{ffbbb57a-07f3-4d5c-97b0-e8e302247c7a}",
                    "myvaultbrowser", "Vault");
            _myVaultBrowser.ShowTitleBar = true;
            _myVaultBrowser.DisabledDockingStates = DockingStateEnum.kDockBottom | DockingStateEnum.kDockTop;

            if (!_myVaultBrowser.IsCustomized)
            {
                _myVaultBrowser.DockingState = DockingStateEnum.kDockRight;
                _myVaultBrowser.Visible = true;
            }

            _applicationEvents.OnActiveProjectChanged += ApplicationEvents_OnActiveProjectChanged;

            if (_activeProjectType == MultiUserModeEnum.kVaultMode)
            {
                SubscribeEvents();
                if (_inventorApplication.Ready)
                    ReloadVaultAddin();
                else
                    _applicationEvents.OnReady += ApplicationEvents_OnReady;
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

            _hwndDic = null;
            Hook.Clear();

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

        private void ApplicationEvents_OnReady(EventTimingEnum BeforeOrAfter, NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            ReloadVaultAddin();
            _applicationEvents.OnReady -= ApplicationEvents_OnReady;
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnActiveProjectChanged(DesignProject ProjectObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
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
            if (DockableWindow.InternalName == "myvaultbrowser")
            {
                if (BeforeOrAfter == EventTimingEnum.kBefore && _inventorApplication.ActiveDocument != null)
                    RestoreVaultBrowser(_inventorApplication.ActiveDocument);
            }
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void DockableWindowsEvents_OnShow(DockableWindow DockableWindow, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            Debug.WriteLine("DockableWindowsEvents_OnShow");
            if (DockableWindow.InternalName == "myvaultbrowser")
            {
                Document doc = _inventorApplication.ActiveDocument;
                if (BeforeOrAfter == EventTimingEnum.kBefore && doc != null && _hwndDic.ContainsKey(doc))
                    UpdateMyVaultBrowser(doc);
            }
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnCloseView(View ViewObject, EventTimingEnum BeforeOrAfter, NameValueMap Context,
            out HandlingCodeEnum HandlingCode)
        {
            Debug.WriteLine(
                $"OnCloseView: {BeforeOrAfter}, Document: {ViewObject.Document.DisplayName}, Number of Views: {ViewObject.Document.Views.Count}");
            if (BeforeOrAfter == EventTimingEnum.kBefore)
                if (ViewObject.Document.Views.Count == 1)
                    _hwndDic.Remove(ViewObject.Document);
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnDeactivateDocument(_Document DocumentObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            Debug.WriteLine(
                $"OnDeactivateDocument: {BeforeOrAfter}, Document: {DocumentObject.DisplayName}, Number of Views: {DocumentObject.Views.Count}");
            if (BeforeOrAfter == EventTimingEnum.kBefore)
            {
                if (_myVaultBrowser.Visible)
                    RestoreVaultBrowser(DocumentObject);
            }
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnActivateDocument(_Document DocumentObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            if (BeforeOrAfter == EventTimingEnum.kAfter)
            {
                if (_hwndDic.ContainsKey(DocumentObject))
                {
                    if (DocumentObject == _inventorApplication.ActiveDocument)
                    {
                        if (_myVaultBrowser.Visible)
                            UpdateMyVaultBrowser(DocumentObject);
                        else
                            DocumentObject.BrowserPanes["Vault"].Visible = true;
                    }
                }
                else
                {
                    Hook.AddDocument(DocumentObject);
                }
            }
            Debug.WriteLine(
                $"OnActivateDocument: {BeforeOrAfter}, Document: {DocumentObject.DisplayName}, Number of Views: {DocumentObject.Views.Count}");
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        #endregion Event Handlers
    }
}
