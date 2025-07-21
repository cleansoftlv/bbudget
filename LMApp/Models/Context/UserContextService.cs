using BlazorApplicationInsights.Interfaces;
using BootstrapBlazor.Components;
using LMApp.Models.Account;
using LMApp.Models.Categories;
using LMApp.Models.Extensions;
using LMApp.Models.Transactions;
using LMApp.Pages;
using Microsoft.AspNetCore.Components;
using Shared;
using Shared.License;
using Shared.LMApi;
using Shared.Login;
using System.Net.Http;
using System.Net.Http.Json;

public class UserContextService(
    IHttpClientFactory httpFactory,
    CascadingValueSource<UserContext> currentAccountSource,
    CascadingValueSource<AuthUserInfo> currentAuthSource,
    LicenseCheckService licenseCheck,
    LocalStorageService localStorageService,
    IApplicationInsights appInsights,
    NavigationManager navManager
    )
{
    private const string ActiveAccountLSKey = "activeAccountId";
    private readonly LicenseCheckService _licenseCheck = licenseCheck;
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly CascadingValueSource<UserContext> _currentAccountSource = currentAccountSource;
    private readonly CascadingValueSource<AuthUserInfo> _currentAuthSource = currentAuthSource;
    private readonly LocalStorageService _localStorageService = localStorageService;
    private readonly IApplicationInsights _appInsights = appInsights;
    private readonly NavigationManager _navManager = navManager;

    private UserContext _account;
    public UserContext CurrentAccount
    {
        get
        {
            if (NotStable && DateTime.UtcNow.Second % 3 == 0)
            {
                return null;
            }

            return _account;
        }
        set
        {
            _account = value;
        }
    }

    public AuthUserInfo AuthInfo
    {
        get;
        private set;
    }

    public LMAccountSettings CurrentAccountSettings
    {
        get;
        private set;
    }

    public LMApp.Models.Account.CategoryItem[] Categories
    {
        get;
        private set;
    }

    public LMApp.Models.Account.AccountDto[] Accounts
    {
        get;
        private set;
    }

    public LMApp.Models.Account.PlaidAccountDto[] PlaidAccounts
    {
        get;
        private set;
    }

    public LMApp.Models.Account.CryptoAccountDto[] CryptoAccounts
    {
        get;
        private set;
    }


    public string ActivePageTitle { get; private set; } = null;

    public EventCallback? OnActivePageBack { get; private set; }

    public bool ActivePageShowBackButton { get; private set; }

    public event Action OnActivePageChange;

    public void SetActivePageState(string title, bool showBackButton)
    {
        ActivePageTitle = title;
        ActivePageShowBackButton = showBackButton;
        OnActivePageChange?.Invoke();
    }

    public SimpleTransactionForEdit TransactionForCreateTransfer { get; set; }

    public bool NotStable { get; set; }

    public void ClearActivePageState()
    {
        SetActivePageState(null, false);
    }

    public void SetActivePageBackHandler(EventCallback? onBack = null)
    {
        OnActivePageBack = onBack;
        OnActivePageChange?.Invoke();
    }


    public async Task<CategoryItem[]> LoadCategories(string token)
    {
        var client = _httpFactory.CreateClient("LMAuth");

        using var request = new HttpRequestMessage(HttpMethod.Get, "categories?format=flattened");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var httpResp = await client.SendAsync(request);
        await httpResp.EnsureSuccessStatusCodeIncludeBody();

        var res = await httpResp.Content.ReadFromJsonAsync<GetCategoriesResponse>();
        if (res == null)
        {
            throw new Exception("Failed to load categories. Response is empty.");
        }

        return res.categories;
    }

    public async Task<AccountDto[]> LoadAccounts(string token)
    {
        var client = _httpFactory.CreateClient("LMAuth");

        using var request = new HttpRequestMessage(HttpMethod.Get, "assets");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var httpResp = await client.SendAsync(request);
        await httpResp.EnsureSuccessStatusCodeIncludeBody();
        var res = await httpResp.Content.ReadFromJsonAsync<GetAccountsResponse>();
        if (res == null)
        {
            throw new Exception("Failed to load accounts. Response is empty.");
        }
        return res.assets;
    }

    public async Task<PlaidAccountDto[]> LoadPlaidAccounts(string token)
    {
        var client = _httpFactory.CreateClient("LMAuth");

        using var request = new HttpRequestMessage(HttpMethod.Get, "plaid_accounts");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var httpResp = await client.SendAsync(request);
        await httpResp.EnsureSuccessStatusCodeIncludeBody();
        var res = await httpResp.Content.ReadFromJsonAsync<GetPlaidAccountsResponse>();
        if (res == null)
        {
            throw new Exception("Failed to load plaid accounts. Response is empty.");
        }
        return res.plaid_accounts;
    }

    public async Task<CryptoAccountDto[]> LoadCryptoAccounts(string token)
    {
        var client = _httpFactory.CreateClient("LMAuth");

        using var request = new HttpRequestMessage(HttpMethod.Get, "crypto");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var httpResp = await client.SendAsync(request);
        await httpResp.EnsureSuccessStatusCodeIncludeBody();
        var res = await httpResp.Content.ReadFromJsonAsync<GetCryptoAccountsResponse>();
        if (res == null)
        {
            throw new Exception("Failed to load crypto accounts. Response is empty.");
        }
        return res.crypto;
    }


    public async Task<UserContext> LoadContext(string token)
    {
        var client = _httpFactory.CreateClient("LMAuth");

        var request = new HttpRequestMessage(HttpMethod.Get, "me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        if (user == null)
        {
            throw new Exception("Failed to load user");
        }

        return new UserContext(user, token);
    }

    public async Task<LMAccountInfo> AddLMAccount(UserContext context)
    {
        var lmAccount = new LMAccountInfo
        {
            Token = context.ApiToken,
            IsActive = context.IsActive,
            UserName = context.UserName,
            Name = context.BudgetName,
            LMAccountId = context.AccountId
        };
        var resp = await SaveNewLMAccount(lmAccount);
        lmAccount.Id = resp.Id;
        lmAccount.LicenseTill = resp.LicenseTill;
        lmAccount.IsPaidLicense = resp.IsPaidLicense;
        if (!CheckAuthSignValid(AuthInfo.Id,
            resp.NewSignature,
            AuthInfo.Accounts.Append(lmAccount)))
        {
            await LoadAndStoreCurrentAuthUser(force: true, allowSaveToSettings: true);
        }
        else
        {
            AuthInfo.Signature = resp.NewSignature;
            AuthInfo.Accounts.Add(lmAccount);
            if (lmAccount.IsActive)
            {
                await SelectActiveAccountFromAuthInfo(lmAccount.Id, freshContext: context, allowSaveToSettings: true);
            }
        }
        return lmAccount;
    }

    private bool CheckAuthSignValid(int id, RequestSignature newSignature, IEnumerable<LMAccountInfo> updatedAccounts)
    {
        var calcSign = _licenseCheck.SignLowSecurity(
                   id,
                   /*Do not change, old clients*/"GetUserAuthInfo",
                   newSignature.Timestamp,
                   newSignature.Nonce,
                   LicensingConstants.LicensingKey + "4!k",
                   updatedAccounts.Select(x => x.LicenseTill)
                   );

        return calcSign == newSignature.Signature;
    }

    public async Task RefreshAccounts()
    {
        if (CurrentAccount == null)
        {
            return;
        }
        var accountsTask = LoadAccounts(CurrentAccount.ApiToken);
        var plaidAccountsTask = LoadPlaidAccounts(CurrentAccount.ApiToken);
        var cryptoAccountsTask = LoadCryptoAccounts(CurrentAccount.ApiToken);

        await Task.WhenAll(accountsTask, plaidAccountsTask, cryptoAccountsTask);

        Accounts = await accountsTask;
        PlaidAccounts = await plaidAccountsTask;
        CryptoAccounts = await cryptoAccountsTask;
    }

    public async Task RemoveLMAccount(LMAccountInfo context)
    {
        var resp = await DeleteLMAccount(context.Id);

        if (!CheckAuthSignValid(AuthInfo.Id,
            resp.NewSignature,
            AuthInfo.Accounts.Where(x => x.Id != context.Id)))
        {
            await LoadAndStoreCurrentAuthUser(force: true, allowSaveToSettings: true);
        }
        else
        {
            AuthInfo.Accounts = AuthInfo.Accounts.Where(x => x.Id != context.Id).ToList();
            AuthInfo.Signature = resp.NewSignature;
            if (CurrentAccount != null && CurrentAccount.ApiToken == context.Token)
            {
                await SelectActiveAccountFromAuthInfo(allowSaveToSettings: true);
            }
        }
    }

    public async Task ResetAuthInfoIfRequired(RequestSignature newSignature)
    {
        if (AuthInfo == null)
        {
            return;
        }

        var calcSign = _licenseCheck.SignLowSecurity(
                   AuthInfo.Id,
                   /*Do not change, old clients*/"GetUserAuthInfo",
                   newSignature.Timestamp,
                   newSignature.Nonce,
                   LicensingConstants.LicensingKey + "4!k",
                   AuthInfo.Accounts.Select(x => x.LicenseTill)
                   );

        var isValid = calcSign == newSignature.Signature;
        if (!isValid)
        {
            await LoadAndStoreCurrentAuthUser(force: true, allowSaveToSettings: true);
        }
    }

    public async Task RemoveSso(SsoInfo sso)
    {
        await DeleteSso(sso.Provider, sso.SsoId);
        AuthInfo.SsoAccounts = AuthInfo.SsoAccounts.Where(x => x.Provider != sso.Provider || x.SsoId != sso.SsoId).ToList();
    }

    public async Task ActivateLMAccount(LMAccountInfo lmAccount)
    {
        await _localStorageService.Save(ActiveAccountLSKey, lmAccount.Id.ToString());
        await SaveActiveLMAccount(lmAccount.Id);
        AuthInfo.Accounts.ForEach(x => x.IsActive = x.Id == lmAccount.Id);
        await SelectActiveAccountFromAuthInfo(allowSaveToSettings: true);
    }

    private async Task DoSwitchAccount(
        UserContext context,
        LMAccountSettings accountSettings,
        AccountsAndCateogries accAndCat,
        bool allowSaveToSettings
        )
    {
        CurrentAccount = context;
        Categories = accAndCat?.Categories;
        Accounts = accAndCat?.Accounts;
        PlaidAccounts = accAndCat?.PlaidAccounts;
        CryptoAccounts = accAndCat?.CryptoAccounts;

        CurrentAccountSettings = accountSettings;
        if (AuthInfo?.Accounts != null)
        {
            AuthInfo.Accounts.ForEach(x => x.IsActive = x.Id == context.Id);
        }
        if (allowSaveToSettings)
        {
            if (CurrentAccount != null)
            {
                await _localStorageService.Save(ActiveAccountLSKey, CurrentAccount.Id.ToString());
            }
            else
            {
                await _localStorageService.Remove(ActiveAccountLSKey);
            }
        }
        await _currentAccountSource.NotifyChangedAsync(context);
    }

    public void EnsureCorrectAccountIdInUrl()
    {
        var urlAccountId = _navManager.GetQueryStringOrDefault<long?>("bid");
        if (urlAccountId != CurrentAccount?.AccountId)
        {
            _navManager.NavigateTo(_navManager.GetUriWithQueryParameter("bid", CurrentAccount?.AccountId), false, true);
        }
    }

    public async ValueTask<AuthUserInfo> LoadAndStoreCurrentAuthUser(bool force = false, bool allowSaveToSettings = false)
    {
        if (AuthInfo != null && !force)
        {
            return AuthInfo;
        }

        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.GetAsync("auth/me");
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return null;
        }

        resp.EnsureSuccessStatusCodeNamed("Api");
        var res = await resp.Content.ReadFromJsonAsync<AuthUserInfo>();
        AuthInfo = res;
        await _appInsights.SetAuthenticatedUserContext(res.Id.ToString());
        await _currentAuthSource.NotifyChangedAsync(AuthInfo);
        await SelectActiveAccountFromAuthInfo(null, null, GetLMAccountIdFromUrl(), allowSaveToSettings);
        return res;
    }

    private long? GetLMAccountIdFromUrl()
    {
        return _navManager.GetQueryStringOrDefault<long?>("bid");
    }

    public async Task<AuthUserInfo> Login()
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.PostAsync("auth/login", null);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return null;
        }
        resp.EnsureSuccessStatusCodeNamed("Api");
        return await resp.Content.ReadFromJsonAsync<AuthUserInfo>();
    }

    public async Task<bool> CheckStaticAppAuth()
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.GetAsync("auth/staticauthcheck");
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return false;
        }
        resp.EnsureSuccessStatusCodeNamed("Api");
        var okres = await resp.Content.ReadFromJsonAsync<OkOnly>();
        return okres != null && okres.ok;
    }

    public async Task<AddSsoResponse> AddSso()
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.PostAsync("auth/addsso", null);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
            || resp.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
            return null;
        }
        resp.EnsureSuccessStatusCodeNamed("Api");
        return await resp.Content.ReadFromJsonAsync<AddSsoResponse>();
    }

    public async Task<AuthUserInfo> LmTokenLogin(string token)
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.PostAsJsonAsync("auth/lmtokenlogin", new LmTokenLoginRequest
        {
            token = token
        });
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return null;
        }
        resp.EnsureSuccessStatusCodeNamed("Api");
        return await resp.Content.ReadFromJsonAsync<AuthUserInfo>();
    }

    public async Task<AddSsoResponse> AddLmSso(string token)
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.PostAsJsonAsync("auth/lmaddsso", new LmTokenLoginRequest
        {
            token = token
        });
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
            || resp.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
            return null;
        }
        resp.EnsureSuccessStatusCodeNamed("Api");
        return await resp.Content.ReadFromJsonAsync<AddSsoResponse>();
    }

    public async Task Logout()
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.PostAsync("auth/logout", null);
        resp.EnsureSuccessStatusCodeNamed("Api");
        var res = await resp.Content.ReadFromJsonAsync<OkOnly>();
        if (res == null || !res.ok)
        {
            throw new HttpRequestException("Failed to logout. Response is empty.", null, System.Net.HttpStatusCode.ExpectationFailed);
        }
    }

    public async Task DeleteProfile()
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.PostAsync("auth/deleteprofile", null);
        resp.EnsureSuccessStatusCodeNamed("Api");
        var res = await resp.Content.ReadFromJsonAsync<OkOnly>();
        if (res == null || !res.ok)
        {
            throw new HttpRequestException("Failed to delete profile. Response is empty.", null, System.Net.HttpStatusCode.ExpectationFailed);
        }
    }

    public async Task<AddLmAccountResponse> SaveNewLMAccount(LMAccountInfo lmAccount)
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.PostAsJsonAsync("auth/lmaccount", lmAccount);
        resp.EnsureSuccessStatusCodeNamed("Api");
        return await resp.Content.ReadFromJsonAsync<AddLmAccountResponse>();
    }

    private async Task SaveActiveLMAccount(int id)
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.PostAsync($"auth/lmaccount/{id}/activate", null);
        resp.EnsureSuccessStatusCodeNamed("Api");
    }

    private async Task<SignedResponse> DeleteLMAccount(int id)
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.DeleteAsync($"auth/lmaccount/{id}", CancellationToken.None);
        resp.EnsureSuccessStatusCodeNamed("Api");
        var res = await resp.Content.ReadFromJsonAsync<SignedResponse>();
        return res;
    }

    private async Task DeleteSso(string provider, string ssoId)
    {
        var client = _httpFactory.CreateClient("Api");
        using var resp = await client.PostAsJsonAsync($"auth/deletesso", new SsoIdInfo
        {
            SsoId = ssoId,
            Provider = provider
        });
        resp.EnsureSuccessStatusCodeNamed("Api");
    }

    public async Task SetAuthInfo(AuthUserInfo authUserInfo)
    {
        AuthInfo = authUserInfo;
        if (authUserInfo.Accounts != null && authUserInfo.Accounts.Any())
        {
            await SelectActiveAccountFromAuthInfo(allowSaveToSettings: true);
        }
        await _currentAuthSource.NotifyChangedAsync(AuthInfo);
    }

    public async Task SelectActiveAccountFromAuthInfo(
        int? id = null,
        UserContext freshContext = null,
        long? lmAccountId = null,
        bool allowSaveToSettings = true)
    {
        if (AuthInfo == null)
        {
            await DoSwitchAccount(null, null, null, allowSaveToSettings);
            return;
        }
        LMAccountInfo currentAccount = null;
        if (id != null)
        {
            currentAccount = AuthInfo.Accounts.FirstOrDefault(x => x.Id == id);
        }
        else if (lmAccountId != null)
        {
            currentAccount = AuthInfo.Accounts.FirstOrDefault(x => x.LMAccountId == lmAccountId);
        }
        else
        {
            var idStr = await _localStorageService.Load(ActiveAccountLSKey);
            if (idStr != null && int.TryParse(idStr, out var idValue))
            {
                currentAccount = AuthInfo.Accounts.FirstOrDefault(x => x.Id == idValue);
            }
        }
        if (currentAccount == null)
        {
            currentAccount = AuthInfo.Accounts.FirstOrDefault(x => x.IsActive);
        }
        if (currentAccount == null)
        {
            currentAccount = AuthInfo.Accounts.FirstOrDefault();
        }
        if (currentAccount == null)
        {
            await DoSwitchAccount(null, null, null, allowSaveToSettings);
            return;
        }
        var classifiersTask = LoadClassifiers(currentAccount.Token);
        var accountTask = freshContext != null ?
            Task.FromResult(freshContext) :
            LoadContext(currentAccount.Token);

        await Task.WhenAll(classifiersTask, accountTask);

        var updatedAccount = await accountTask;
        updatedAccount.Id = currentAccount.Id;
        updatedAccount.LicenseExpirationDate = currentAccount.LicenseTill;

        var classifiers = await classifiersTask;
        var settings = currentAccount.Settings ?? new LMAccountSettings();

        await DoSwitchAccount(updatedAccount, settings, classifiers, allowSaveToSettings);
    }


    private async Task<AccountsAndCateogries> LoadClassifiers(string apiToken)
    {
        var categoriesTask = LoadCategories(apiToken);
        var assetsTask = LoadAccounts(apiToken);
        var plaidAccountsTask = LoadPlaidAccounts(apiToken);
        var cryptoAccountsTask = LoadCryptoAccounts(apiToken);

        await Task.WhenAll(categoriesTask, assetsTask, plaidAccountsTask, cryptoAccountsTask);

        return new AccountsAndCateogries
        {
            Categories = (await categoriesTask).Where(x => !x.is_group).ToArray(),
            Accounts = await assetsTask,
            PlaidAccounts = await plaidAccountsTask,
            CryptoAccounts = await cryptoAccountsTask
        };
    }
}