using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using InvAddIn;
using Inventor;

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
        private static MultiUserModeEnum activeProjectType;
        private static ApplicationEvents applicationEvents;
        private static DockableWindowsEvents dockableWindowsEvents;
        private static Application inventorApplication;
        private static DockableWindow myVaultBrowser;

        private static View activeView;
        
        // Dictionary to store the HWND of the view and the corresponding HWND of the vault browser.
        private static Dictionary<int, IntPtr> viewHwndDic;

        public static void AddVaultBrowserHwnd(int view, IntPtr hwnd)
        {
            viewHwndDic[view] = hwnd;

            //if (activeView.HWND == view)
            //    vaultBrowserPane = inventorApplication.ActiveDocument.BrowserPanes["Vault"];
            if (myVaultBrowser.Visible)
            {
                UpdateVaultBrowser();
            }
        }

        private static void UpdateVaultBrowser()
        {
            myVaultBrowser.Clear();
            if (viewHwndDic.ContainsKey(activeView.HWND))
            {
                activeView.Document.BrowserPanes["Vault"].Visible = false;
                myVaultBrowser.AddChild(viewHwndDic[activeView.HWND]);
            }
        }

        private static void RestoreVaultBrowser()
        {
            myVaultBrowser.Clear();
            if (activeView.Document.BrowserPanes.Count >= 2)
                activeView.Document.BrowserPanes["Vault"].Visible = true;
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
            try
            {
                ApplicationAddIn vaultAddin =
                    inventorApplication.ApplicationAddIns.ItemById["{48B682BC-42E6-4953-84C5-3D253B52E77B}"];
                if (vaultAddin.Activated)
                {
                    vaultAddin.Deactivate();
                    vaultAddin.Activate();
                }
            }
            catch
            {
                //
            }
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
                SubscribeEvents();

            viewHwndDic = new Dictionary<int, IntPtr>();

            applicationEvents.OnActiveProjectChanged += ApplicationEvents_OnActiveProjectChanged;

            // TODO: Add ApplicationAddInServer.Activate implementation.
            // e.g. event initialization, command creation etc.

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

            
        }

        public void Deactivate()
        {
            // Release objects.
            if (activeProjectType == MultiUserModeEnum.kVaultMode)
                UnSubscribeEvents();
            applicationEvents.OnActiveProjectChanged -= ApplicationEvents_OnActiveProjectChanged;

            dockableWindowsEvents = null;
            applicationEvents = null;

            myVaultBrowser = null;

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

        #endregion

        #region Event Handlers

        private void ApplicationEvents_OnActiveProjectChanged(DesignProject ProjectObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            if (activeProjectType != ProjectObject.ProjectType)
            {
                if (ProjectObject.ProjectType == MultiUserModeEnum.kVaultMode)
                {
                    SubscribeEvents();
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
                viewHwndDic.ContainsKey(activeView.HWND))
                UpdateVaultBrowser();
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
                viewHwndDic.Remove(ViewObject.HWND);
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnNewView(View ViewObject, EventTimingEnum BeforeOrAfter, NameValueMap Context,
            out HandlingCodeEnum HandlingCode)
        {
            if (BeforeOrAfter == EventTimingEnum.kAfter)
            {
                var doc = ViewObject.Document;
                if (doc.Views.Count > 1)
                    if (viewHwndDic.ContainsKey(doc.Views[1].HWND))
                        viewHwndDic[ViewObject.HWND] = viewHwndDic[doc.Views[1].HWND];
            }
#if DEBUG
            //if (ViewObject != null)
            //    Debug.WriteLine($"OnNewView: {BeforeOrAfter}, Document: {ViewObject.Document.DisplayName}, " +
            //                    $"View: {ViewObject.HWND}, activeview: {activeView.HWND}, actualview:{inventorApplication.ActiveView.HWND}");
            //else
            //    try
            //    {
            //    Debug.WriteLine($"OnNewView: {BeforeOrAfter}, Document: {inventorApplication.ActiveDocument.DisplayName}, " +
            //                    $"activeview: {activeView.HWND}, actualview:{inventorApplication.ActiveView.HWND}");
            //    }
            //    catch
            //    {
            //        Debug.WriteLine($"OnNewView: {BeforeOrAfter}, Document: None, " +
            //                        $"activeview: None, actualview: None");
            //    }
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
            //Debug.WriteLine($"OnActivateView: {BeforeOrAfter}, Document: {ViewObject.Document.DisplayName}, " +
            //                $"View: {ViewObject.HWND}, activeview: {activeView.HWND}, actualview:{inventorApplication.ActiveView.HWND}");
#endif
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

        private void ApplicationEvents_OnDeactivateDocument(_Document DocumentObject, EventTimingEnum BeforeOrAfter,
            NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
#if DEBUG
            //Debug.WriteLine($"OnDeactivateDocument: {BeforeOrAfter}, Document: {DocumentObject.DisplayName}, " +
            //                $"activeview: { activeView.HWND}, actualview: { inventorApplication.ActiveView.HWND}");
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

                if (!viewHwndDic.ContainsKey(viewHwnd))
                    Win32Api.SetEventHook((uint) viewHwnd, (uint) Process.GetCurrentProcess().Id);
                else
                {
                    if (myVaultBrowser.Visible && DocumentObject.Equals(inventorApplication.ActiveDocument))
                        UpdateVaultBrowser();
                }
            }
#if DEBUG
            //Debug.WriteLine($"OnActivateDocument: {BeforeOrAfter}, Document: {DocumentObject.DisplayName}, " +
            //                $"activeview: {activeView.HWND}, actualview: {inventorApplication.ActiveView.HWND}");
#endif
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
        }

#endregion 

    }
}