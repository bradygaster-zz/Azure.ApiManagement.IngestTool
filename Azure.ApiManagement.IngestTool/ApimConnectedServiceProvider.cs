using Azure.ApiManagement.IngestTool.ViewModels;
using Microsoft.VisualStudio.ConnectedServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Azure.ApiManagement.IngestTool
{
    [ConnectedServiceProviderExport(Constants.CONNECTED_SERVICE_NAME)]
    internal class ApimConnectedServiceProvider : ConnectedServiceProvider
    {
        [Import(AllowDefault = true)]
        public Lazy<ConnectedServicesManager> ConnectedServicesManager { get; set; }

        public ApimConnectedServiceProvider()
        {
            Category = Resources.ConSvcCategory;
            Name = Resources.ConSvcName;
            Description = Resources.ConSvcDescription;
            CreatedBy = "Microsoft";
            Version = new Version(1, 0, 0);
            MoreInfoUri = new Uri("http://microsoft.com");
            Icon = Imaging
                .CreateBitmapSourceFromHBitmap(
                    Resources.API_Management.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(64, 64)
                );
        }

        public override Task<ConnectedServiceConfigurator> CreateConfiguratorAsync(ConnectedServiceProviderContext context)
        {
            ConnectedServiceInstance instance = new ConnectedServiceInstance();

            var wizard = new WizardViewModel();
            wizard.Pages.Add(new SelectApiAppViewModel(context, instance));
            wizard.Pages.Add(new ApiManagementIngestViewModel(context, instance));

            ConnectedServiceConfigurator configurator = wizard as ConnectedServiceConfigurator;

            return Task.FromResult(configurator);
        }
    }
}
