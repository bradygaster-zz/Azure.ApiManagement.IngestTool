using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ConnectedServices;
using Azure.ApiManagement.IngestTool.Views;

namespace Azure.ApiManagement.IngestTool.ViewModels
{
    public class WizardViewModel : ConnectedServiceWizard
    {
        public WizardViewModel()
        {
        }

        private void Authenticator_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }

        public override Task<ConnectedServiceInstance> GetFinishedServiceInstanceAsync()
        {
            ConnectedServiceInstance instance = new ConnectedServiceInstance();
            return Task.FromResult(instance);
        }
    }
}
