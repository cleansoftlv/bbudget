using BlazorApplicationInsights.Interfaces;
using BootstrapBlazor.Components;
using LMApp.Models.Account;
using LMApp.Models.Extensions;
using LMApp.Models.UI;
using Microsoft.AspNetCore.Components;
using Shared;
using Shared.License;
using System.Threading;
using System.Timers;
using Toolbelt.Blazor.ViewTransition;

namespace LMApp.Pages
{
    public class BasePage : ComponentBase, IDisposable
    {

        [Inject]
        public IViewTransition ViewTransition { get; set; }

        [Inject]
        public NavigationManager navigationManager { get; set; }

        [Inject]
        public LicenseCheckService licenseCheck { get; set; }

        [Inject]
        public UserContextService userService { get; set; }

        [Inject]
        public Utils utils { get; set; }

        [Inject]
        public ILogger<BasePage> log { get; set; }

        [CascadingParameter] public IModalContainer ModalContainer { get; set; }

        [CascadingParameter]
        public BreakPoint CurrentBreakPoint { get; set; }

        private System.Timers.Timer _timer;

        public virtual bool AuthRequired => true;
        public virtual bool AccountRequired => true;
        public virtual bool SettingsRequired => true;

        protected bool LoadCancelled = false;

        protected bool IsLoading { get; set; }
        protected string LoadError { get; set; }

        private async void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            await InvokeAsync(() =>
            {
                if (userService.CurrentAccount == null
                    || userService.AuthInfo == null)
                    return;

                var accountInAuthInfo = userService.AuthInfo?.Accounts
                    .FirstOrDefault(x => x.Id == userService.CurrentAccount.Id);

                if (accountInAuthInfo == null
                    || accountInAuthInfo.LicenseTill != userService.CurrentAccount.LicenseExpirationDate
                    || string.IsNullOrEmpty(userService.AuthInfo.Signature?.Signature))
                {
                    userService.NotStable = true;
                    return;
                }

                var calcSign = licenseCheck.SignLowSecurity(
                    userService.AuthInfo.Id,
                    /*Do not change, old clients*/"GetUserAuthInfo",
                    userService.AuthInfo.Signature.Timestamp,
                    userService.AuthInfo.Signature.Nonce,
                    LicensingConstants.LicensingKey + "4!k",
                    userService.AuthInfo.Accounts.Select(x => x.LicenseTill)
                    );

                if (calcSign != userService.AuthInfo.Signature.Signature)
                {
                    userService.NotStable = true;
                }
            });
        }

        private bool _reloadingAccount = false;
        protected async Task<bool> EnsureCorrectAccountLoaded()
        {
            var lmAccountId = userService.GetLMAccountIdFromUrl();
            if (userService.CurrentAccount != null
                && lmAccountId != null
                && lmAccountId != userService.CurrentAccount.AccountId
                && !_reloadingAccount)
            {
                _reloadingAccount = true;
                await userService.SelectActiveAccountFromAuthInfo(
                    null, 
                    null, 
                    lmAccountId, 
                    allowSaveToSettings: true);
                _reloadingAccount = false;
                return false;
            }

            return true;
        }

        protected override async Task OnInitializedAsync()
        {
            userService.ClearActivePageState();
            IsLoading = true;
            LoadError = null;
            if (AuthRequired)
            {
                try
                {
                    var auth = await userService.LoadAndStoreCurrentAuthUser();
                    if (auth == null && !navigationManager.UriStartsWith("login"))
                    {
                        LoadCancelled = true;
                        navigationManager.NavigateTo("/login?cb=noauth");
                        return;
                    }
                    _timer = new System.Timers.Timer(5000);
                    _timer.Elapsed += OnTimerEvent;
                    _timer.AutoReset = true;
                    _timer.Start();
                }
                catch (HttpRequestException e) when (e.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    LoadCancelled = true;
                    e.LogIfRequired(log);
                    LoadError = e.GetDescriptionForUser();
                    IsLoading = false;
                    navigationManager.NavigateTo("/login?cb=noauth");
                    return;
                }
                catch (HttpRequestException e)
                {
                    e.LogIfRequired(log);
                    LoadCancelled = true;
                    LoadError = e.GetDescriptionForUser();
                    IsLoading = false;
                    return;
                }
            }
            else
            {
                try
                {
                    await userService.LoadAndStoreCurrentAuthUser();
                }
                catch {
                    //ignore error, auth not required
                }
            }

            if (AccountRequired)
            {
                var urlAccountId = userService.GetLMAccountIdFromUrl();

                var account = userService.CurrentAccount;

                if (urlAccountId != null
                    && account != null
                    && account.AccountId != urlAccountId)
                {
                    await userService.SelectActiveAccountFromAuthInfo(
                        null,
                        null,
                        urlAccountId,
                        allowSaveToSettings: true);
                    account = userService.CurrentAccount;
                }

                if (account == null)
                {
                    LoadCancelled = true;
                    navigationManager.NavigateTo("/auth?noAuth=true");
                    return;
                }

                if (account.LicenseExpirationDate < DateTime.UtcNow)
                {
                    LoadCancelled = true;
                    navigationManager.NavigateTo("/auth?noLicense=true");
                    return;
                }

                userService.EnsureCorrectAccountIdInUrl();
            }
            if (SettingsRequired)
            {
                if (userService.CurrentAccountSettings?.TransferCategoryId == null)
                {
                    LoadCancelled = true;
                    IsLoading = false;
                    LoadError = "Settings are not configured";
                    navigationManager.NavigateTo("/settings?noSettings=true");
                    return;
                }
            }
            IsLoading = false;
        }

        private bool _transtionStarted;

        [Inject]
        public ILogger<BasePage> Logger { get; set; }
        protected ValueTask StartTransition()
        {
            if (_transtionStarted)
            {
                return ValueTask.CompletedTask;
            }
            _transtionStarted = true;
            Logger.LogInformation("Starting transition");
            return ViewTransition.BeginAsync();
        }

        protected async Task EndTransition()
        {
            if (!_transtionStarted)
            {
                return;
            }
            StateHasChanged();
            await Task.Yield();
            await ViewTransition.EndAsync();
            _transtionStarted = false;
        }

        protected ValueTask StartTransition(BreakPoint min)
        {
            if (CurrentBreakPoint < min)
            {
                return ValueTask.CompletedTask;
            }
            return StartTransition();
        }

        protected void ReloadOnError()
        {
            navigationManager.Refresh();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await utils.Init();
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private int NavStackSize = 0;


        protected async ValueTask ResponsiveNavigate(string url, NavDirection direction)
        {
            if (direction == NavDirection.None)
            {
                navigationManager.NavigateTo(url,
                    forceLoad: false,
                    replace: true);
                return;
            }

            if (CurrentBreakPoint >= BreakPoint.Large)
            {
                NavStackSize = 0;
                navigationManager.NavigateTo(url,
                    forceLoad: false,
                    replace: true);
                return;
            }

            if (direction == NavDirection.Back && NavStackSize > 0)
            {
                await utils.HistoryBack();
                NavStackSize -= 1;
            }
            else
            {
                navigationManager.NavigateTo(url,
                    forceLoad: false,
                    replace: false);

                if (direction == NavDirection.Forward)
                {
                    NavStackSize += 1;
                }
            }
        }

    }
}
