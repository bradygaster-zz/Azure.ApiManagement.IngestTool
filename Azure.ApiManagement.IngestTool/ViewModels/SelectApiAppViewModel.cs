using Azure.ApiManagement.IngestTool.Views;
using Microsoft.VisualStudio.ConnectedServices;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.WindowsAzure.Authentication;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure;
using Microsoft.Azure.Management.Resources.Models;
using System.Windows.Threading;
using Azure.ApiManagement.IngestTool.Utility;
using Microsoft.VisualStudio.Services.Client.AccountManagement;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Azure.ApiManagement.IngestTool.ViewModels
{
    public class SelectApiAppViewModel : ConnectedServiceWizardPage
    {
        public SelectApiAppViewModel(ConnectedServiceProviderContext context,
            ConnectedServiceInstance instance)
        {
            Context = context;
            Title = Resources.Page1Title;
            Description = Resources.Page1Subtitle;
            Legend = Resources.Page1Legend;

            View = new SelectApiAppView
            {
                DataContext = this
            };

            _dispatcher = Dispatcher.CurrentDispatcher;

            ApiApps = new List<ApiAppResource>();
            Instance = instance;
        }

        public ConnectedServiceInstance Instance { get; set; }
        public ConnectedServiceProviderContext Context { get; set; }

        private Account _account;
        public Account Account
        {
            get { return _account; }
            set
            {
                _account = value;
                OnPropertyChanged();
            }
        }

        private List<ApiAppResource> _apiApps;
        public List<ApiAppResource> ApiApps
        {
            get { return _apiApps; }
            set
            {
                _apiApps = value;
                OnPropertyChanged();

                if (value != null)
                {
                    SelectedApiApp = value.FirstOrDefault();
                }
            }
        }

        private ApiAppResource _selectedApiApp;
        public ApiAppResource SelectedApiApp
        {
            get { return _selectedApiApp; }
            set
            {
                _selectedApiApp = value;
                OnPropertyChanged();
                OnApiAppSelected(value);

                if (value != null)
                {
                    if (!Context.Args.ContainsKey(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_API_APP))
                        Context.Args.Add(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_API_APP, value);
                    else
                        Context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_API_APP] = value;
                }
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
                GetApiAppsInSubscription();
            }
        }

        private string Token { get; set; }

        private Dispatcher _dispatcher;

        private async void GetApiAppsInSubscription()
        {
            if (Wizard.Pages.Any(x => x.Legend == Resources.Page1Legend && x.IsSelected))
            {
                using (Context.StartBusyIndicator(Resources.WaitMessageGettingApiApps))
                {
                    IAzureRMUserAccountSubscriptionContext sub = _selectedSubscription;
                    if (sub == null)
                    {
                        ApiApps = new List<ApiAppResource>();
                        return;
                    }

                    Token = await sub.GetAuthenticationHeaderAsync(true);
                    Token = Token.Substring("Bearer ".Length);

                    var apiApps = new List<ApiAppResource>();

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                        var url = string.Format(Constants.URL_APP_SERVICES_WITH_METADATA_DEFINITION, sub.Subscription.SubscriptionId);
                        var json = httpClient.GetStringAsync(new Uri(url)).Result;
                        var job = JObject.Parse(json);
                        var apps = (JArray)job["value"];

                        foreach (var app in apps)
                        {
                            try
                            {
                                apiApps.Add(new ApiAppResource
                                {
                                    Name = app["name"].Value<string>(),
                                    ResourceGroup = Utility.ResourceUtilities.GetResourceGroupFromResourceId(
                                        app["id"].Value<string>()
                                        ),
                                    ResourceId = app["id"].Value<string>(),
                                    SwaggerUrl = app["properties"]["siteConfig"]["apiDefinition"]["url"].Value<string>()
                                });
                            }
                            catch
                            {

                            }
                        }
                    }

                    _dispatcher.Invoke(() => ApiApps = apiApps);
                }

                if (ApiApps.Any())
                {
                    Wizard.IsNextEnabled = true;
                }
            }
        }

        private async void OnApiAppSelected(ApiAppResource value)
        {
            if (value == null)
                return;

            if (Wizard.Pages.Any(x => x.Legend == Resources.Page1Legend && x.IsSelected))
            {
                using (Context.StartBusyIndicator(Resources.WaitMessageGettingSwagger))
                {
                    using (var httpClient = new HttpClient())
                    {
                        var swaggerJson = await httpClient.GetStringAsync(value.SwaggerUrl);

                        if (!Context.Args.ContainsKey(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_SWAGGER))
                            Context.Args.Add(Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_SWAGGER, swaggerJson);
                        else
                            Context.Args[Constants.CONNECTED_SERVICE_METADATA_KEY_FOR_SWAGGER] = swaggerJson;
                    }
                }
            }
        }
    }
}
