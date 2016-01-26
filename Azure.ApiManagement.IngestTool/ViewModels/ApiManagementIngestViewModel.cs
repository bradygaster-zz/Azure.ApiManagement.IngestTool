using Azure.ApiManagement.IngestTool.Utility;
using Azure.ApiManagement.IngestTool.Views;
using Microsoft.Azure;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Resources.Models;
using Microsoft.VisualStudio.ConnectedServices;
using Microsoft.VisualStudio.Services.Client.AccountManagement;
using Microsoft.VisualStudio.WindowsAzure.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Azure.Management.ApiManagement;
using Microsoft.Azure.Management.ApiManagement.SmapiModels;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Azure.ApiManagement.IngestTool.ViewModels
{
    public class ApiManagementIngestViewModel : ConnectedServiceWizardPage
    {
        public ApiManagementIngestViewModel(ConnectedServiceProviderContext context,
            ConnectedServiceInstance instance)
        {
            Context = context;
            Title = Resources.Page2Title;
            Description = Resources.Page2Subtitle;
            Legend = Resources.Page2Legend;

            View = new ApiManagementIngestView
            {
                DataContext = this
            };

            _dispatcher = Dispatcher.CurrentDispatcher;

            Products = new ObservableCollection<ApiManagementProduct>();
            Products.CollectionChanged += Products_CollectionChanged;
            Instance = instance;
        }

        public ConnectedServiceProviderContext Context { get; set; }
        public ResourceManagementClient ResourceManagementClient { get; set; }
        public ApiManagementClient ApiManagementClient { get; set; }
        public ConnectedServiceInstance Instance { get; set; }

        private Account _account;
        private Dispatcher _dispatcher;

        public Account Account
        {
            get { return _account; }
            set
            {
                _account = value;
                OnPropertyChanged();
            }
        }

        private string _token;
        public string Token
        {
            get { return _token; }
            set
            {
                _token = value;

                if (!Context.Args.ContainsKey(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_ADAL_TOKEN))
                    Context.Args.Add(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_ADAL_TOKEN, Token);
                else
                    Context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_ADAL_TOKEN] = Token;
            }
        }

        private IReadOnlyList<IAzureRMUserAccountSubscriptionContext> _subscriptions;
        public IReadOnlyList<IAzureRMUserAccountSubscriptionContext> Subscriptions
        {
            get { return _subscriptions; }
            set
            {
                _subscriptions = value;
                OnPropertyChanged();

                if (_subscriptions != null)
                {
                    SelectedSubscription = _subscriptions.FirstOrDefault();
                    GetApiManagementResources();
                }
            }
        }

        private IAzureRMUserAccountSubscriptionContext _selectedSubscription;
        public IAzureRMUserAccountSubscriptionContext SelectedSubscription
        {
            get { return _selectedSubscription; }
            set
            {
                _selectedSubscription = value;
                OnPropertyChanged();

                if(value != null)
                {
                    if (!Context.Args.ContainsKey(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_SUB_ID))
                        Context.Args.Add(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_SUB_ID, value.Subscription.SubscriptionId);
                    else
                        Context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_SUB_ID] = value.Subscription.SubscriptionId;

                    GetApiManagementResources();
                }
            }
        }

        private List<ApiManagementInstance> _apimInstances;
        public List<ApiManagementInstance> ApiManagementInstances
        {
            get { return _apimInstances; }
            set
            {
                _apimInstances = value;
                OnPropertyChanged();

                if (value != null)
                {
                    SelectedApiManagementInstance = value.FirstOrDefault();
                }
            }
        }

        private ObservableCollection<ApiManagementProduct> _products;
        public ObservableCollection<ApiManagementProduct> Products
        {
            get { return _products; }
            set
            {
                _products = value;
                OnPropertyChanged();
            }
        }

        private ApiManagementInstance _SelectedApiManagementInstance;
        public ApiManagementInstance SelectedApiManagementInstance
        {
            get { return _SelectedApiManagementInstance; }
            set
            {
                _SelectedApiManagementInstance = value;
                OnPropertyChanged();

                if (value != null)
                {
                    GetApiManagementProducts();

                    if (!Context.Args.ContainsKey(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_APIM_INSTANCE))
                        Context.Args.Add(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_APIM_INSTANCE, SelectedApiManagementInstance);
                    else
                        Context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_APIM_INSTANCE] = SelectedApiManagementInstance;
                }
            }
        }

        private async Task GetApiManagementResources()
        {
            if (Wizard.Pages.First(x => x.Legend == Resources.Page2Legend).IsSelected)
            {
                using (Context.StartBusyIndicator(Resources.WaitMessageGettingApimList))
                {
                    IAzureRMUserAccountSubscriptionContext sub = _selectedSubscription;
                    if (sub == null)
                    {
                        return;
                    }

                    Token = await sub.GetAuthenticationHeaderAsync(true);
                    Token = Token.Substring("Bearer ".Length);

                    ResourceManagementClient = new ResourceManagementClient(
                        new TokenCloudCredentials(SelectedSubscription.Subscription.SubscriptionId, Token)
                    );

                    var resources = await ResourceManagementClient.Resources.ListAsync(new ResourceListParameters
                    {
                        ResourceType = Constants.APIM_RESOURCE_TYPE
                    });

                    var apims = new List<ApiManagementInstance>();

                    foreach (var resource in resources.Resources)
                    {
                        apims.Add(new ApiManagementInstance
                        {
                            Name = resource.Name,
                            ResourceId = resource.Id
                        });
                    }

                    _dispatcher.Invoke(() => ApiManagementInstances = apims);
                }
            }
        }

        private void Products_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (ApiManagementProduct option in e.NewItems)
                    option.PropertyChanged += Option_PropertyChanged;

            if (e.OldItems != null)
                foreach (ApiManagementProduct option in e.OldItems)
                    option.PropertyChanged -= Option_PropertyChanged;
        }

        private void Option_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.Wizard.IsNextEnabled = Products.Any(x => x.IsChecked == true);
            base.Wizard.IsFinishEnabled = Products.Any(x => x.IsChecked == true);

            if (!Context.Args.ContainsKey(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_PRODUCTS))
                Context.Args.Add(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_PRODUCTS, Products.Where(x => x.IsChecked == true).AsEnumerable());
            else
                Context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_PRODUCTS] = Products.Where(x => x.IsChecked == true).AsEnumerable();
        }

        private async Task GetApiManagementProducts()
        {
            if (Wizard.Pages.First(x => x.Legend == Resources.Page2Legend).IsSelected)
            {
                using (Context.StartBusyIndicator(Resources.WaitMessageRetrievingProductList))
                {
                    IAzureRMUserAccountSubscriptionContext sub = _selectedSubscription;
                    if (sub == null)
                    {
                        return;
                    }

                    Token = await sub.GetAuthenticationHeaderAsync(true);
                    Token = Token.Substring("Bearer ".Length);

                    ApiManagementClient = new ApiManagementClient(
                        new TokenCloudCredentials(SelectedSubscription.Subscription.SubscriptionId, Token)
                    );

                    var productResponse = await ApiManagementClient.Products.ListAsync(
                            ResourceUtilities.GetResourceGroupFromResourceId(SelectedApiManagementInstance.ResourceId),
                            SelectedApiManagementInstance.Name,
                            new QueryParameters()
                            );

                    var products = new List<ApiManagementProduct>();

                    foreach (var product in productResponse.Result.Values)
                    {
                        products.Add(new ApiManagementProduct
                        {
                            Name = product.Name,
                            Id = product.Id
                        });
                    }

                    _dispatcher.Invoke(() =>
                    {
                        Products.Clear();
                        products.ForEach(p => Products.Add(p));
                    });
                }
            }
        }
    }
}
