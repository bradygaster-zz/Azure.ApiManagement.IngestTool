//------------------------------------------------------------------------------
// <copyright file="StartApimImportWizardCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ConnectedServices;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ComponentModelHost;
using EnvDTE;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using System.IO;
using System.Linq;

namespace Azure.ApiManagement.IngestTool
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class StartApimImportWizardCommand
    {
        [Import]
        public ConnectedServicesManager ConnectedServicesManager { get; set; }

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1bee8b9c-c40d-4bf8-8735-445c945f5f1b");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartApimImportWizardCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private StartApimImportWizardCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private Project GetSelectedProject(IServiceProvider provider)
        {
            // Get the DTE service and make sure there is an open solution
            DTE dte = provider.GetService(typeof(DTE)) as DTE;
            if (dte == null || dte.Solution == null)
            {
                return null;
            }

            Project project = null;
            IntPtr selectionHierarchy = IntPtr.Zero;
            IntPtr selectionContainer = IntPtr.Zero;

            // Get the current selection in the shell
            IVsMonitorSelection monitorSelection = provider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            if (monitorSelection != null)
            {
                try
                {
                    uint itemId;
                    IVsMultiItemSelect multiSelect;

                    monitorSelection.GetCurrentSelection(out selectionHierarchy, out itemId, out multiSelect, out selectionContainer);
                    if (selectionHierarchy != IntPtr.Zero)
                    {
                        IVsHierarchy hierarchy = (IVsHierarchy)Marshal.GetObjectForIUnknown(selectionHierarchy);
                        return GetProjectFromHierarchy(hierarchy);
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    // Make sure we release the COM pointers in any case
                    if (selectionHierarchy != IntPtr.Zero)
                    {
                        Marshal.Release(selectionHierarchy);
                    }
                    if (selectionContainer != IntPtr.Zero)
                    {
                        Marshal.Release(selectionContainer);
                    }
                }
            }

            return null;
        }

        private Project GetProjectFromHierarchy(IVsHierarchy hierarchy)
        {
            object obj;

            int hr = hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out obj);
            if (ErrorHandler.Succeeded(hr))
            {
                return obj as Project;
            }

            return null;
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            Project project = GetSelectedProject(ServiceProvider);
            var button = (OleMenuCommand)sender;

            button.Visible = true;
            button.Enabled = false;

            if (project == null)
            {
                return;
            }

            string pathToProject = Path.GetDirectoryName(project.FullName);
            string pathToProfiles = Path.Combine(pathToProject, "Properties\\PublishProfiles");

            if (Directory.Exists(pathToProfiles))
            {
                var pubxml = Directory.GetFiles(pathToProfiles, "*.pubxml");
                if (pubxml != null && pubxml.Any())
                {
                    foreach (var profile in pubxml)
                    {
                        var domainName = GetDomainNameFromPubXml(profile);

                        if (domainName.IndexOf("azurewebsites.net") > 0)
                        {
                            button.Visible = true;
                            button.Enabled = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static StartApimImportWizardCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new StartApimImportWizardCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            var projects = (Array)dte.ActiveSolutionProjects;

            foreach (var item in projects)
            {
                Project project = (EnvDTE.Project)item;
                IVsSolution solution = (IVsSolution)ServiceProvider.GetService(typeof(IVsSolution));
                IVsHierarchy hierarchy = null;
                solution.GetProjectOfUniqueName(project.UniqueName, out hierarchy);

                if (this.ConnectedServicesManager == null)
                    (Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel)
                        .DefaultCompositionService.SatisfyImportsOnce(this);

                ConnectedServicesManager.ConfigureServiceAsync(Constants.CONNECTED_SERVICE_NAME, hierarchy);
            }
        }

        static string GetDomainNameFromPubXml(string filename)
        {
            var nsMsBuild = "http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument doc = XDocument.Load(filename);
            return doc
                .Element(XName.Get("Project", nsMsBuild))
                    .Element(XName.Get("PropertyGroup", nsMsBuild))
                        .Element(XName.Get("SiteUrlToLaunchAfterPublish", nsMsBuild))
                            .Value;
        }

        static ProjectItem FindPublishProfiles(ProjectItem projectItem)
        {
            foreach (var item in projectItem.ProjectItems)
            {
                ProjectItem i = (ProjectItem)item;

                if (i.Name.EndsWith("pubxml"))
                {
                    return i;
                }
                else
                {
                    if (i.ProjectItems.Count > 0)
                    {
                        return FindPublishProfiles(i);
                    }
                }
            }

            return null;
        }
    }
}
