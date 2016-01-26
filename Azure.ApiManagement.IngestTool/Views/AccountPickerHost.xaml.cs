using Microsoft.VisualStudio.Services.Client.AccountManagement;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.WindowsAzure.Authentication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Azure.ApiManagement.IngestTool.Views
{
    public partial class AccountPickerHost : UserControl
    {
        public AccountPickerHost()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }
    
        public static readonly DependencyProperty AccountProperty = DependencyProperty.Register(
            "Account", typeof(Account), typeof(AccountPickerHost), new PropertyMetadata(default(Account)));

        public static readonly DependencyProperty IsLoadingSubscriptionsProperty = DependencyProperty.Register(
            "IsLoadingSubscriptions", typeof(bool), typeof(AccountPickerHost), new PropertyMetadata(false));

        public static readonly DependencyProperty SubscriptionsProperty = DependencyProperty.Register(
            "Subscriptions", typeof(IReadOnlyList<IAzureRMUserAccountSubscriptionContext>), typeof(AccountPickerHost), new PropertyMetadata(default(IReadOnlyList<IAzureRMUserAccountSubscriptionContext>)));

        private static readonly IReadOnlyList<IAzureRMUserAccountSubscriptionContext> NoSubscriptions = new List<IAzureRMUserAccountSubscriptionContext>();
        private IWpfAccountPicker _accountPicker;
        private IAzureAuthenticationManager _authenticationManager;
        private IAzureRMAuthenticationManager _rmAuthenticationManager;

        public Account Account
        {
            get { return (Account)GetValue(AccountProperty); }
            set { SetValue(AccountProperty, value); }
        }

        public bool IsLoadingSubscriptions
        {
            get { return (bool)GetValue(IsLoadingSubscriptionsProperty); }
            set { SetValue(IsLoadingSubscriptionsProperty, value); }
        }

        public IReadOnlyList<IAzureRMUserAccountSubscriptionContext> Subscriptions
        {
            get { return (IReadOnlyList<IAzureRMUserAccountSubscriptionContext>)GetValue(SubscriptionsProperty); }
            set { SetValue(SubscriptionsProperty, value); }
        }

        private IAzureAuthenticationManager AzureAuthenticationManager
        {
            get { return _authenticationManager ?? (_authenticationManager = ServiceProvider.GlobalProvider.GetService(typeof(IAzureAuthenticationManager)) as IAzureAuthenticationManager); }
        }

        private IAzureRMAuthenticationManager AzureRMAuthenticationManager
        {
            get
            {
                EnsureRMAuthenticationManager();
                return _rmAuthenticationManager;
            }
        }

        private void EnsureRMAuthenticationManager()
        {
            if (_rmAuthenticationManager == null)
            {
                _rmAuthenticationManager = ServiceProvider.GlobalProvider.GetService(typeof(IAzureRMAuthenticationManager)) as IAzureRMAuthenticationManager;

                if (_rmAuthenticationManager != null)
                {
                    _rmAuthenticationManager.SubscriptionsChanged += OnSubscriptionsChanged;
                }
            }
        }

        private async void InjectAccountPickerAsync()
        {
            IVsAccountManagementService vsAccountManagementService = ServiceProvider.GlobalProvider.GetService(typeof(SVsAccountManagementService)) as IVsAccountManagementService;

            if (vsAccountManagementService == null)
            {
                return;
            }

            AccountPickerOptions options = new AccountPickerOptions(Window.GetWindow(this), "VS Azure Tooling")
            {
                IsCompactHeight = false,
                UseWindowsPresentationFoundationStyle = false,
                IsAuthenticationStateUIEnabled = true
            };

            EnsureRMAuthenticationManager();
            _accountPicker = await vsAccountManagementService.CreateWpfAccountPickerAsync(options);
            AccountPickerHostControl.Content = _accountPicker.Control;
            _accountPicker.PropertyChanged += OnAccountPickerPropertyChanged;
            _accountPicker.SelectedAccount = await AzureAuthenticationManager.GetCurrentVSAccountAsync();
            UpdateSettings();
        }

        private void OnAccountPickerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedAccount")
            {
                UpdateSettings();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            InjectAccountPickerAsync();
        }

        private async void OnSubscriptionsChanged(object sender, EventArgs e)
        {
            List<IAzureRMUserAccountSubscriptionContext> subscriptions = (await AzureRMAuthenticationManager.GetSubscriptionsAsync()).ToList();
            Account account = (Account)_accountPicker.SelectedAccount;

            if (subscriptions.Any(x => x.UserAccount.UniqueId != account.UniqueId))
            {
                Dispatcher.Invoke(() => { Subscriptions = subscriptions.ToList(); });
            }
        }

        private async void UpdateSettings()
        {
            Account account = (Account)_accountPicker.SelectedAccount;
            IReadOnlyList<IAzureRMUserAccountSubscriptionContext> subscriptions;

            if (account != null)
            {
                IAzureAuthenticationManager manager = AzureAuthenticationManager;

                if (manager != null)
                {
                    await manager.SetCurrentVSAccountAsync(account);
                }

                Dispatcher.Invoke(() => { IsLoadingSubscriptions = true; });

                subscriptions = (await AzureRMAuthenticationManager.GetSubscriptionsAsync()).ToList();

                Dispatcher.Invoke(() => { IsLoadingSubscriptions = false; });
            }
            else
            {
                subscriptions = NoSubscriptions;
            }

            Dispatcher.Invoke(() =>
            {
                Account = account;
                Subscriptions = subscriptions.ToList();
            });
        }
    }
}
