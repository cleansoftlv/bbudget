using BootstrapBlazor.Components;
using LMApp.Models.Account;
using LMApp.Models.Categories;
using LMApp.Models.Extensions;
using LMApp.Models.Transactions;
using Shared;
using Shared.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Context
{
    public class SettingsService(
        IHttpClientFactory httpFactory,
        LocalStorageService localStorage,
        UserContextService userService)
    {
        private const string LocalSettingsKey = "LocalSettings";
        private readonly IHttpClientFactory _httpFactory = httpFactory;
        private readonly UserContextService _userService = userService;
        private readonly LocalStorageService _localStorage = localStorage;

        public UserContext CurrentAccount => _userService.CurrentAccount;

        public CategoryItem GetCachedCategory(long categoryId)
        {
            return _userService.Categories?.FirstOrDefault(c => c.id == categoryId);
        }

        public IEnumerable<CategoryItem> GetCachedCategories()
        {
            return _userService.Categories?.OrderBy(x => x.name);
        }

        public IEnumerable<UnifiedAccountInfo> GetCachedAccountsForLookup()
        {
            var assets = _userService.Accounts.Where(x =>
                !x.exclude_transactions
                && x.closed_on == null)
                .Select(ConvertAccount);

            var plaid = _userService.PlaidAccounts
                .Select(ConvertAccount);

            return assets.Concat(plaid).OrderBy(x => x.Name);
        }

        public IEnumerable<AccountDto> GetUnlilteredAccounts()
        {
            return _userService.Accounts;
        }

        public UnifiedAccountInfo FindAssetOrPlaidAccount(string accountUid)
        {
            var parsed = TransactionsService.ParseTranAccountUid(accountUid);
            if (parsed == default) {
                return null;
            }
            return FindAssetOrPlaidAccount(parsed.assetId, parsed.plaidAccountId);
        }

        public UnifiedAccountInfo FindAssetOrPlaidAccount(long? assetId, long? plaidAccountId)
        {
            if (assetId.HasValue)
            {
                return _userService.Accounts.Where(x => x.id == assetId.Value)
                    .Select(ConvertAccount).FirstOrDefault();
            }
            else if (plaidAccountId.HasValue)
            {
                return _userService.PlaidAccounts.Where(x => x.id == plaidAccountId.Value)
                    .Select(ConvertAccount).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        public UnifiedAccountInfo ConvertAccount(AccountDto dto)
        {
            return new UnifiedAccountInfo
            {
                IdForType = dto.id,
                Name = dto.name ?? dto.display_name,
                AccountType = AccountType.Default,
                Currency = dto.currency
            };
        }

        public UnifiedAccountInfo ConvertAccount(PlaidAccountDto dto)
        {
            return new UnifiedAccountInfo
            {
                IdForType = dto.id,
                Name = dto.name ?? dto.display_name,
                AccountType = AccountType.Plaid,
                Currency = dto.currency
            };
        }


        public async Task<LocalSettings> GetLocalSettings()
        {
            return await _localStorage.LoadObject<LocalSettings>(LocalSettingsKey) ??
                LocalSettings.CreateDefault();
        }

        public async Task SaveLocalSettings(LocalSettings settings)
        {
            await _localStorage.SaveObject(LocalSettingsKey, settings);
        }

        public async Task SaveAccountSettings(int id, LMAccountSettings settings)
        {
            var client = _httpFactory.CreateClient("Api");
            using var resp = await client.PostAsJsonAsync($"auth/lmaccount/{id}/settings", settings);
            resp.EnsureSuccessStatusCodeNamed("Api");
        }

        public async Task SaveCurrentSettings()
        {
            if (_userService.CurrentAccount != null)
            {
                await SaveAccountSettings(_userService.CurrentAccount.Id, _userService.CurrentAccountSettings ?? new LMAccountSettings());
            }
        }

        public string PrimaryCurrency => _userService.CurrentAccount?.PrimaryCurrency;

        public string[] AdditionalCurrencies => _userService.CurrentAccountSettings?.AdditionalCurrencies;


        private static readonly Color[] _currencyColors
            = [Color.None, Color.Primary, Color.Secondary, Color.Success, Color.Warning, Color.Info, Color.Light, Color.Dark];

        public Color GetCurrencyColor(string currency)
        {
            currency = currency?.ToUpperInvariant();
            int index;
            if (currency == PrimaryCurrency)
            {
                index = 1;
            }
            else
            {
                var addIndex = Array.IndexOf(AdditionalCurrencies, currency);
                if (addIndex < 0)
                {
                    index = 0;
                }
                else
                {
                    index = addIndex + 2;
                }
            }
            index = index % _currencyColors.Length;

            return _currencyColors[index];
        }

        public IEnumerable<string> SelectedAccountCurrencies
        {
            get
            {

                yield return PrimaryCurrency;

                foreach (var currency in AdditionalCurrencies)
                {
                    yield return currency;
                }
            }
        }

        public string[] AllSupportedCurrencies => _allCurrencies;

        public LMAccountSettings Settings => _userService.CurrentAccountSettings;

        public static LMAccountSettings CreateDefaultSettings() => new LMAccountSettings
        {
            SortTransactionOnLoadMore = true
        };


        private static string[] _allCurrencies = ["AED",
"AFN",
"ALL",
"AMD",
"ANG",
"AOA",
"ARS",
"AUD",
"AWG",
"AZN",
"BAM",
"BBD",
"BDT",
"BGN",
"BHD",
"BIF",
"BMD",
"BND",
"BOB",
"BRL",
"BSD",
"BTC",
"BTN",
"BWP",
"BYN",
"BZD",
"CAD",
"CDF",
"CHF",
"CLP",
"CNY",
"COP",
"CRC",
"CUC",
"CUP",
"CVE",
"CZK",
"DJF",
"DKK",
"DOP",
"DZD",
"EGP",
"ERN",
"ETB",
"EUR",
"FJD",
"FKP",
"GBP",
"GEL",
"GGP",
"GHS",
"GIP",
"GMD",
"GNF",
"GTQ",
"GYD",
"HKD",
"HNL",
"HRK",
"HTG",
"HUF",
"IDR",
"ILS",
"IMP",
"INR",
"IQD",
"IRR",
"ISK",
"JEP",
"JMD",
"JOD",
"JPY",
"KES",
"KGS",
"KHR",
"KMF",
"KPW",
"KRW",
"KWD",
"KYD",
"KZT",
"LAK",
"LBP",
"LKR",
"LRD",
"LSL",
"LTL",
"LVL",
"LYD",
"MAD",
"MDL",
"MGA",
"MKD",
"MMK",
"MNT",
"MOP",
"MRO",
"MUR",
"MVR",
"MWK",
"MXN",
"MYR",
"MZN",
"NAD",
"NGN",
"NIO",
"NOK",
"NPR",
"NZD",
"OMR",
"PAB",
"PEN",
"PGK",
"PHP",
"PKR",
"PLN",
"PYG",
"QAR",
"RON",
"RSD",
"RUB",
"RWF",
"SAR",
"SBD",
"SCR",
"SDG",
"SEK",
"SGD",
"SHP",
"SLL",
"SOS",
"SRD",
"STD",
"SVC",
"SYP",
"SZL",
"THB",
"TJS",
"TMT",
"TND",
"TOP",
"TRY",
"TTD",
"TWD",
"TZS",
"UAH",
"UGX",
"USD",
"UYU",
"UZS",
"VEF",
"VND",
"VUV",
"WST",
"XAF",
"XCD",
"XOF",
"XPF",
"YER",
"ZAR",
"ZMW",
"ZWL",
];

    }
}
