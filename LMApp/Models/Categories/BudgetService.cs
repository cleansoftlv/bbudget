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

            var response = await lmClient.GetFromJsonAsync<BudgetCategory[]>($"budgets?start_date={monthStart:yyyy-MM-dd}&end_date={monthEnd:yyyy-MM-dd}");
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

        public bool IsLiability(LMAccountType accountType)
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
    }
}
