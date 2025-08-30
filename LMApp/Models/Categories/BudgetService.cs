using LMApp.Models.Account;
using LMApp.Models.Context;
using LMApp.Models.Transactions;
using LMApp.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LMApp.Models.Categories
{
    public class BudgetService(
        IHttpClientFactory httpClientFactory,
        TransactionsService transactionService,
        UserContextService userContextService,
        SettingsService settingsService)
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly TransactionsService _transactionService = transactionService;
        private readonly UserContextService _userContextService = userContextService;
        private readonly SettingsService _settingsService = settingsService;

        public static long TotalCategoryId = -1;
        public static long TotalAccountId = -1;

        private long _cachedAccountId = -1;
        private readonly Dictionary<DateTime, BudgetCategoryDisplay[]> _budgetCache = new Dictionary<DateTime, BudgetCategoryDisplay[]>();
        public async Task<(BudgetCategoryDisplay[] categories, bool cacheHit)> LoadBudget(DateTime monthStart, bool allowCache = false)
        {
            if (_cachedAccountId != _userContextService.CurrentAccount.AccountId)
            {
                _budgetCache.Clear();
                _cachedAccountId = _userContextService.CurrentAccount.AccountId;
            }

            if (allowCache && _budgetCache.TryGetValue(monthStart, out var cached))
            {
                return (cached, true);
            }

            var lmClient = _httpClientFactory.CreateClient("LM");
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            BudgetCategory[] response = null;
            try
            {
                response = await lmClient.GetFromJsonAsync<BudgetCategory[]>($"budgets?start_date={monthStart:yyyy-MM-dd}&end_date={monthEnd:yyyy-MM-dd}");
            }
            catch (JsonException x)
            {
                throw new HttpRequestException("Error response",
                    x,
                    System.Net.HttpStatusCode.ExpectationFailed);
            }
            if (response == null)
            {
                throw new HttpRequestException("Empty response",
                    null,
                    System.Net.HttpStatusCode.ExpectationFailed);
            }

            var categories = response
                .Where(x => x.ShowInBudget)
                .Select(x => x.GetDisplayItem(_settingsService.PrimaryCurrency));

            var withoutIncome = categories.Where(x => x.CategoryType == BudgetCategoryType.Expense);

            var total = new BudgetCategoryDisplay
            {
                Name = "All expenses",
                CategoryType = BudgetCategoryType.Total,
                CategoryId = TotalCategoryId,
                BudgetedAmountPrimary = withoutIncome.Sum(x => x.BudgetedAmountPrimary),
                ActualAmountPrimary = withoutIncome.Sum(x => x.ActualAmountPrimary),
                PrimaryCurrency = _settingsService.PrimaryCurrency,
                ProgressAmountPrimary = withoutIncome.Sum(x => x.ProgressAmountPrimary),
            };

            total.BudgetedAmount = total.BudgetedAmountPrimary;
            total.Currency = _settingsService.PrimaryCurrency;

            var res = new[] { total }
                .Concat(categories.OrderBy(x => x.CategoryType).ThenBy(x => x.Name)).ToArray();

            _budgetCache[monthStart] = res;

            return (res, false);
        }

        public Task<GetTransactionsResult> LoadCategoryTransactions(
            long categoryId,
            int offset = 0,
            DateTime? endDate = null)
        {
            return categoryId == TotalCategoryId
                ? _transactionService.GetAllTransactionsAsync(offset, endDate)
                : _transactionService.GetTransactionsForCategoryAsync(categoryId, offset, endDate);
        }

        public Task<GetTransactionsResult> LoadAccountTransactions(
            long idForType,
            AccountType accountType,
            int offset = 0,
            DateTime? endDate = null)
        {
            return accountType switch
            {
                AccountType.Default => _transactionService.GetTransactionsForAccountAsync(idForType, offset, endDate),
                AccountType.Plaid => _transactionService.GetTransactionsForPlaidAccountAsync(idForType, offset, endDate),
                AccountType.Crypto => Task.FromResult(new GetTransactionsResult
                {
                    Transactions = new List<TransactionDisplay>(),
                    HasMore = false,
                }),
                AccountType.Total => _transactionService.GetAllTransactionsAsync(offset, endDate),
                _ => throw new ArgumentOutOfRangeException(nameof(accountType), accountType, null)
            };
        }

        public async Task<AccountDisplay[]> LoadAccounts()
        {
            await _userContextService.RefreshAccounts();
            return GetCachedAccounts();
        }

        public AccountDisplay[] GetCachedAccounts()
            => ConvertFilterAddTotal(_settingsService.GetUnlilteredAccounts(), _userContextService.PlaidAccounts, _userContextService.CryptoAccounts).ToArray();

        private IEnumerable<AccountDisplay> ConvertFilterAddTotal(
            IEnumerable<AccountDto> dtos,
            IEnumerable<PlaidAccountDto> plaidDtos,
            IEnumerable<CryptoAccountDto> cryptoDtos)
        {
            var accounts = dtos
                .Where(x => x.closed_on == null)
                .Select(Convert)
                .Concat(plaidDtos.Select(Convert))
                .Concat(cryptoDtos.Select(Convert));

            var total = new AccountDisplay
            {
                IdForType = TotalAccountId,
                Name = "Total",
                Currency = _settingsService.PrimaryCurrency,
                Balance = accounts.Sum(x => x.BalancePrimaryCurrency),
                AccountType = AccountType.Total
            };

            if (accounts.Any(x => x.IsLiability)
                && accounts.Any(x => !x.IsLiability))
            {
                total.AssetsBalance = accounts
                    .Where(x => !x.IsLiability)
                    .Sum(x => x.BalancePrimaryCurrency);
                total.LiabilitiesBalance = accounts
                    .Where(x => x.IsLiability)
                    .Sum(x => x.BalancePrimaryCurrency);
            }

            return new[] { total }.Concat(accounts.OrderBy(x => x.Name));
        }
        private AccountDisplay Convert(AccountDto dto)
        {
            var res = new AccountDisplay
            {
                IdForType = dto.id,
                Name = String.IsNullOrEmpty(dto.name)
                    ? dto.display_name
                    : dto.name,
                Currency = dto.currency,
                Balance = dto.balance,
                BalancePrimaryCurrency = dto.to_base ?? 0,
                AccountType = AccountType.Default,
                LMAccountType = ConvertLMAccountType(dto.type_name),
            };

            if (IsLiability(res.LMAccountType))
            {
                res.IsLiability = true;
                res.Balance = -res.Balance;
                res.BalancePrimaryCurrency = -res.BalancePrimaryCurrency;
            }

            return res;
        }

        private AccountDisplay Convert(PlaidAccountDto dto)
        {
            var res = new AccountDisplay
            {
                IdForType = dto.id,
                Name = String.IsNullOrEmpty(dto.name)
                    ? dto.display_name
                    : dto.name,
                Currency = dto.currency,
                Balance = dto.balance,
                BalancePrimaryCurrency = dto.to_base ?? 0,
                AccountType = AccountType.Plaid,
                LMAccountType = ConvertPlaidAccountType(dto.type),
                IsReadonly = true, // Plaid accounts are read-only in LM
            };

            if (IsLiability(res.LMAccountType))
            {
                res.IsLiability = true;
                res.Balance = -res.Balance;
                res.BalancePrimaryCurrency = -res.BalancePrimaryCurrency;
            }

            return res;
        }

        private AccountDisplay Convert(CryptoAccountDto dto)
        {
            var res = new AccountDisplay
            {
                IdForType = dto.zabo_account_id ?? dto.id ?? -9,
                Name = String.IsNullOrEmpty(dto.name)
                    ? dto.display_name
                    : dto.name,
                Currency = dto.currency,
                Balance = dto.balance,
                BalancePrimaryCurrency = dto.to_base ?? 0,
                AccountType = AccountType.Crypto,
                LMAccountType = LMAccountType.Cryptocurrency,
                IsReadonly = true,
                CanHaveTransactions = false
            };

            if (IsLiability(res.LMAccountType))
            {
                res.IsLiability = true;
                res.Balance = -res.Balance;
                res.BalancePrimaryCurrency = -res.BalancePrimaryCurrency;
            }

            return res;
        }

        private LMAccountType ConvertLMAccountType(string type)
        {
            return type switch
            {
                "cash" => LMAccountType.Cash,
                "credit" => LMAccountType.Credit,
                "investment" => LMAccountType.Investment,
                "real estate" => LMAccountType.RealEstate,
                "loan" => LMAccountType.Loan,
                "vehicle" => LMAccountType.Vehicle,
                "cryptocurrency" => LMAccountType.Cryptocurrency,
                "employee compensation" => LMAccountType.EmployeeCompensation,
                "other liability" => LMAccountType.OtherLiability,
                "other asset" => LMAccountType.OtherAsset,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private LMAccountType ConvertPlaidAccountType(string type)
        {
            return type switch
            {
                "depository" => LMAccountType.OtherAsset,
                "cash" => LMAccountType.Cash,
                "brokerage" => LMAccountType.Investment,
                "credit" => LMAccountType.Credit,
                "investment" => LMAccountType.Investment,
                "loan" => LMAccountType.Loan,
                _ => LMAccountType.OtherAsset
            };
        }

        public static bool IsLiability(LMAccountType accountType)
        {
            return accountType switch
            {
                LMAccountType.Cash => false,
                LMAccountType.Credit => true,
                LMAccountType.Investment => false,
                LMAccountType.RealEstate => false,
                LMAccountType.Loan => true,
                LMAccountType.Vehicle => false,
                LMAccountType.Cryptocurrency => false,
                LMAccountType.EmployeeCompensation => false,
                LMAccountType.OtherLiability => true,
                LMAccountType.OtherAsset => false,
                _ => throw new ArgumentOutOfRangeException(nameof(accountType), accountType, null)
            };
        }

        /// <summary>
        /// Creates a new asset in Lunch Money
        /// </summary>
        /// <param name="request">The asset creation request</param>
        /// <returns>The created asset as AccountDto</returns>
        /// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
        public async Task<AccountDto> CreateAssetAsync(CreateAssetRequest request)
        {
            var lmClient = _httpClientFactory.CreateClient("LM");

            try
            {
                var response = await lmClient.PostAsJsonAsync("assets", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Failed to create asset. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        response.StatusCode);
                }

                var result = await response.Content.ReadFromJsonAsync<AccountDto>();
                if (result == null || result.id == default)
                {
                    throw new HttpRequestException("Empty response from create asset API",
                        null,
                        System.Net.HttpStatusCode.ExpectationFailed);
                }
                //Work around for wrond type returned by create
                result.type_name = request.type_name;
                // Clear account cache since we added a new asset
                _userContextService.UpdateCachedAccount(result);
                return result;
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException("Error parsing create asset response",
                    ex,
                    System.Net.HttpStatusCode.ExpectationFailed);
            }
        }

        /// <summary>
        /// Updates an existing asset in Lunch Money
        /// </summary>
        /// <param name="assetId">The ID of the asset to update</param>
        /// <param name="request">The asset update request</param>
        /// <returns>The updated asset as AccountDto</returns>
        /// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
        public async Task<AccountDto> UpdateAssetAsync(long assetId, UpdateAssetRequest request)
        {
            var lmClient = _httpClientFactory.CreateClient("LM");

            try
            {
                var response = await lmClient.PutAsJsonAsync($"assets/{assetId}", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Failed to update asset {assetId}. Status: {response.StatusCode}, Content: {errorContent}",
                        null,
                        response.StatusCode);
                }

                var result = await response.Content.ReadFromJsonAsync<AccountDto>();
                if (result == null || result.id == default)
                {
                    throw new HttpRequestException("Empty response from update asset API",
                        null,
                        System.Net.HttpStatusCode.ExpectationFailed);
                }
                //Work around for wrond type returned by update
                if (request.type_name != null)
                    result.type_name = request.type_name;
                // Clear account cache since we updated an asset
                _userContextService.UpdateCachedAccount(result);
                return result;
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException("Error parsing update asset response",
                    ex,
                    System.Net.HttpStatusCode.ExpectationFailed);
            }
        }

        /// <summary>
        /// Helper method to convert AccountDisplay to CreateAssetRequest
        /// </summary>
        /// <param name="account">The account display object to convert</param>
        /// <returns>A CreateAssetRequest object ready for API submission</returns>
        public static CreateAssetRequest ConvertToCreateAssetRequest(AccountDisplay account)
        {
            return new CreateAssetRequest
            {
                type_name = ConvertToLMApiTypeName(account.LMAccountType),
                name = account.Name,
                display_name = account.Name,
                balance = account.Balance,
                currency = account.Currency.ToLowerInvariant(),
                balance_as_of = DateTime.UtcNow,
                exclude_transactions = false
            };
        }

        /// <summary>
        /// Helper method to convert AccountDisplay to UpdateAssetRequest
        /// </summary>
        /// <param name="account">The account display object to convert</param>
        /// <returns>An UpdateAssetRequest object ready for API submission</returns>
        public static UpdateAssetRequest ConvertToUpdateAssetRequest(AccountDisplay account)
        {
            return new UpdateAssetRequest
            {
                type_name = ConvertToLMApiTypeName(account.LMAccountType),
                name = account.Name,
                display_name = account.Name,
                balance = account.Balance,
                currency = account.Currency.ToLowerInvariant(),
                balance_as_of = DateTime.UtcNow,
                exclude_transactions = false
            };
        }

        /// <summary>
        /// Saves a list of edited accounts, creating new ones and updating changed ones
        /// </summary>
        /// <param name="editedAccounts">List of accounts to save</param>
        /// <returns>Task representing the save operation</returns>
        /// <exception cref="HttpRequestException">Thrown when any API request fails</exception>
        public async Task<bool> SaveEditedAccountsAsync(IEnumerable<AccountDisplayForEdit> editedAccounts)
        {
            // Process only accounts that have changes
            var accountsToSave = editedAccounts.Where(a => a.HasChanges()).ToList();
            if (!accountsToSave.Any())
            {
                return false; // No changes to save
            }

            foreach (var account in accountsToSave)
            {
                if (account.IsNewAccount)
                {
                    await CreateAccountFromEditAsync(account);
                }
                else
                {
                    await UpdateAccountFromEditAsync(account);
                }
            }
            return true;
        }

        /// <summary>
        /// Creates a new account from AccountDisplayForEdit
        /// </summary>
        /// <param name="account">The account to create</param>
        /// <returns>The created account as AccountDto</returns>
        /// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
        public async Task<AccountDto> CreateAccountFromEditAsync(AccountDisplayForEdit account)
        {
            var createRequest = new CreateAssetRequest
            {
                type_name = ConvertToLMApiTypeName(account.LMAccountType),
                name = account.Name,
                display_name = account.Name,
                balance = account.Balance,
                currency = account.Currency?.ToLowerInvariant() ?? "usd",
                balance_as_of = DateTime.UtcNow,
                exclude_transactions = false
            };

            if (account.IsLiabilityEdit)
            {
                createRequest.balance *= -1;
            }

            return await CreateAssetAsync(createRequest);
        }

        /// <summary>
        /// Updates an existing account from AccountDisplayForEdit with selective field updates
        /// </summary>
        /// <param name="account">The account to update</param>
        /// <returns>The updated account as AccountDto</returns>
        /// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
        public async Task<AccountDto> UpdateAccountFromEditAsync(AccountDisplayForEdit account)
        {
            if (!account.HasChanges())
            {
                throw new InvalidOperationException($"No changes detected for account {account.Name}");
            }

            // Create update request with only changed fields
            var updateRequest = new UpdateAssetRequest();

            if (account.OriginalName != account.Name)
            {
                updateRequest.name = account.Name;
                updateRequest.display_name = account.Name;
            }

            if (account.Balance != account.OriginalBalance
                || !string.Equals(account.Currency, account.OriginalCurrency)
                || account.IsLiability != account.IsLiabilityEdit)
            {
                updateRequest.currency = account.Currency.ToLowerInvariant();
                updateRequest.balance = account.Balance;
                updateRequest.balance_as_of = DateTime.UtcNow;
                if (account.IsLiabilityEdit)
                {
                    updateRequest.balance *= -1;
                }
            }

            if (account.LMAccountType != account.OriginalType)
            {
                updateRequest.type_name = ConvertToLMApiTypeName(account.LMAccountType);
            }

            return await UpdateAssetAsync(account.IdForType, updateRequest);
        }

        /// <summary>
        /// Converts LMAccountType enum to Lunch Money API type name string
        /// </summary>
        /// <param name="accountType">The LMAccountType enum value</param>
        /// <returns>The corresponding API type name string</returns>
        public static string ConvertToLMApiTypeName(LMAccountType accountType)
        {
            return accountType switch
            {
                LMAccountType.Cash => "cash",
                LMAccountType.Credit => "credit",
                LMAccountType.Investment => "investment",
                LMAccountType.RealEstate => "real estate",
                LMAccountType.Loan => "loan",
                LMAccountType.Vehicle => "vehicle",
                LMAccountType.Cryptocurrency => "cryptocurrency",
                LMAccountType.EmployeeCompensation => "employee compensation",
                LMAccountType.OtherLiability => "other liability",
                LMAccountType.OtherAsset => "other asset",
                _ => "other asset"
            };
        }
    }
}
