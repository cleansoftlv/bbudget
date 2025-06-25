using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Services.DAL;
using Services.DAL.Entities;
using Services.Licensing;
using Services.Options;
using Shared;
using Shared.Helpers;
using Shared.License;
using Shared.LMApi;
using Shared.Login;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Services.Auth
{
    public class AuthService
    {
        private readonly ServicesOptions _options;
        private static readonly string[] second = ["anonymous"];
        private readonly IDbContextFactory<CommonContext> _dbFactory;
        private readonly IHttpClientFactory _httpFactory;
        private readonly LicenseCheckService _licenseCheckService;

        public AuthService(IOptions<ServicesOptions> options,
            IDbContextFactory<CommonContext> dbFactory,
            IHttpClientFactory httpClientFactory,
            LicenseCheckService licenseCheckService)
        {
            _options = options.Value;
            _dbFactory = dbFactory;
            _httpFactory = httpClientFactory;
            _licenseCheckService = licenseCheckService;
        }

        public (ClaimsPrincipal principal, DateTime expirationDate) ValidateJwt(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return default;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Jwt.Secret));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                NameClaimType = "sub",


                ValidIssuer = _options.Jwt.Issuer,
                ValidAudience = _options.Jwt.Audience,
                IssuerSigningKey = key,

                ClockSkew = TimeSpan.FromMinutes(2)
            };
            var res = handler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
            return (res, validatedToken.ValidTo);
        }



        public static int? GetUsetId(ClaimsIdentity identity)
        {
            if (string.IsNullOrEmpty(identity.Name))
                return null;

            if (int.TryParse(identity.Name, out var userId))
                return userId;

            return null;
        }
        public string GenerateJwt(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim("sub", userId.ToString()),
            };
            var identity = new ClaimsIdentity(claims);
            return GenerateJwt(identity);
        }

        public string GenerateJwt(ClaimsIdentity identity)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Jwt.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _options.Jwt.Issuer,
                audience: _options.Jwt.Audience,
                claims: identity.Claims,
                expires: DateTime.UtcNow.AddMinutes(_options.Jwt.ExpirationMinutes),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public int GetCookiesExpirationMinutes()
        {
            return _options.Jwt.ExpirationMinutes;
        }

        public bool NeedsRenew(DateTime expirationDate)
        {
            if (!_options.Jwt.SlidingRenew)
                return false;

            var now = DateTime.UtcNow;
            var minutesTillExpiration = (expirationDate - now).TotalMinutes;
            return minutesTillExpiration < (_options.Jwt.ExpirationMinutes / 2);
        }


        public ClaimsPrincipal GetStaticWebAppPrincipal(string xMsClientPrincipal)
        {
            if (string.IsNullOrEmpty(xMsClientPrincipal))
            {
                return null;
            }

            var decoded = Convert.FromBase64String(xMsClientPrincipal);
            var json = Encoding.UTF8.GetString(decoded);
            var principal = JsonSerializer.Deserialize<StaticAppClientPrincipal>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            principal.UserRoles = principal.UserRoles?.Except(second, StringComparer.CurrentCultureIgnoreCase);
            if (!principal.UserRoles?.Any() ?? true)
            {
                return null;
            }

            var identity = new ClaimsIdentity(principal.IdentityProvider);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
            identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));
            identity.AddClaims(principal.UserRoles.Select(r => new Claim(ClaimTypes.Role, r)));

            return new ClaimsPrincipal(identity);
        }

        public SsoId GetStaticWebAppUserSso(ClaimsPrincipal principal)
        {
            return new SsoId
            {
                Name = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,
                Provider = "sw_" + principal.Identity.AuthenticationType,
                Id = principal.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value
            };
        }

        public async Task<int?> GetUserIdBySsoId(SsoId ssoId)
        {
            using var context = _dbFactory.CreateDbContext();

            return await context.UserSsos
                .Where(x => x.provider == ssoId.Provider && x.sso_id == ssoId.Id)
                .Select(x => (int?)x.user_id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreateNewUserWithSso(SsoId ssoId)
        {
            using var context = _dbFactory.CreateDbContext();
            var now = DateTime.UtcNow;
            var user = new AppUser
            {
                created = now,
                last_activity_date = now,
                name = TextHelper.LimitLength(ssoId.Name, 100),
            };
            context.AppUsers.Add(user);
            await context.SaveChangesAsync();
            await AddSso(user.id, ssoId);
            return user.id;
        }

        public async Task<(SsoAddResult result, SsoId newSsoId)> AddSso(int userId, SsoId ssoId)
        {

            var ssoUserId = await GetUserIdBySsoId(ssoId);
            if (ssoUserId.HasValue)
            {
                if (ssoUserId != userId)
                {
                    bool otherUserHasOtherCredentials = await CheckUserHasOtherCredentials(ssoUserId.Value, ssoId);
                    if (!otherUserHasOtherCredentials)
                        return (SsoAddResult.CannotTakeFromOtherUser, null);
                }

                var updatedSso = await UpdateSso(userId, ssoId);
                if (updatedSso == null)
                {
                    return (SsoAddResult.Error, null);
                }

                if (ssoUserId == userId)
                    return (SsoAddResult.Duplicate, updatedSso);

                return (SsoAddResult.TakenFromOtherUser, updatedSso);
            }
            var userSso = new UserSso
            {
                user_id = userId,
                provider = ssoId.Provider,
                sso_id = ssoId.Id,
                name = TextHelper.LimitLength(ssoId.Name, 250)
            };
            using var db = _dbFactory.CreateDbContext();
            db.UserSsos.Add(userSso);
            await db.SaveChangesAsync();
            return (SsoAddResult.Ok, new SsoId
            {
                Name = userSso.name,
                Provider = userSso.provider,
                Id = userSso.sso_id
            });
        }

        public async Task<bool> DeleteLMAccount(int id, int ownerId)
        {
            using var db = _dbFactory.CreateDbContext();
            var lmAccount = await db.LMAccounts
                .Where(x => x.id == id && x.user_id == ownerId)
                .FirstOrDefaultAsync();

            if (lmAccount == null)
                return false;

            await UpdateAccountLicenseOnRemove_NoSave(db, ownerId, lmAccount.lm_account_id);

            db.LMAccounts.Remove(lmAccount);
            await db.SaveChangesAsync();
            return true;
        }

        private async Task UpdateAccountLicenseOnRemove_NoSave(CommonContext db, int userId, long lmAccountId)
        {
            var accLicense = await db.LMAccountLicenses
               .FirstOrDefaultAsync(x => x.lm_account_id == lmAccountId);

            if (accLicense == null
                || !accLicense.is_paid
                || accLicense.paid_license_id == null)
            {
                return;
            }

            var license = await db.PaidLicenses
                .FirstOrDefaultAsync(x => x.id == accLicense.paid_license_id);

            if (license == null || license.user_id != userId)
            {
                return;
            }

            var now = DateTime.UtcNow;

            var newLicense = await (from pl in db.PaidLicenses
                                    join ua in db.LMAccounts
                                      on pl.user_id equals ua.user_id
                                    where ua.lm_account_id == lmAccountId
                                      && pl.date_to > now
                                      && !pl.is_cancelled
                                      && pl.user_id != userId
                                    orderby pl.date_to descending, pl.id descending
                                    select new
                                    {
                                        pl.id,
                                        pl.date_to,
                                        pl.user_id
                                    }).FirstOrDefaultAsync();

            if (newLicense != null)
            {
                accLicense.date_to = newLicense.date_to;
                accLicense.is_paid = true;
                accLicense.modified = now;
                accLicense.date_from = now;
                accLicense.paid_license_id = newLicense.id;
                accLicense.by_user_id = newLicense.user_id;
            }
            else
            {
                var trailEndDate = accLicense.created.AddDays(_options.TrialLicenseLengthDays);
                accLicense.date_to = trailEndDate;
                accLicense.is_paid = false;
                accLicense.modified = now;
                accLicense.date_from = now;
                accLicense.paid_license_id = null;
                accLicense.by_user_id = userId;
            }
        }

        private string GetLMAccountEncryptPassword(LMAccountInfo account)
        {
            return GetLMAccountEncryptPassword(account.LMAccountId);
        }


        private string GetLMAccountEncryptPassword(long accountId)
        {
            return String.Concat("encode_",
                accountId,
                _options.TokenEncryptSecert,
                accountId);
        }

        public Task<LMAccountInfo> AddLMAccountNonActive(int userId, LMAccountInfo lmAccount)
        {
            lmAccount.IsActive = false;
            return AddLMAccount_NoActiveCheck(userId, lmAccount);
        }

        private async Task<LMAccountInfo> AddLMAccount_NoActiveCheck(int userId, LMAccountInfo lmAccount)
        {
            var row = new LMAccount
            {
                user_id = userId,
                lm_account_id = lmAccount.LMAccountId,
                name = lmAccount.Name,
                token = AesPasswordEncryptionHelper.Encrypt(lmAccount.Token, GetLMAccountEncryptPassword(lmAccount)),
                is_active = lmAccount.IsActive,
                additional_currencies = lmAccount.Settings?.AdditionalCurrencies != null
                    ? string.Join(',', lmAccount.Settings.AdditionalCurrencies.Select(x => x.ToUpperInvariant()))
                    : null,
                user_name = lmAccount.UserName,
                id = 0,
                sort_tran_on_load_more = lmAccount.Settings?.SortTransactionOnLoadMore ?? false,
                transfer_category_id = lmAccount.Settings?.TransferCategoryId,
                cross_currency_transfer_category_id = lmAccount.Settings?.CrossCurrencyTransferCategoryId
            };

            using var db = _dbFactory.CreateDbContext();

            db.LMAccounts.Add(row);
            var license = await EnsureHasCorrectLicense_NoSave(row.lm_account_id, userId, db);
            await db.SaveChangesAsync();
            lmAccount.Id = row.id;
            lmAccount.LicenseTill = license.DateTo;
            lmAccount.IsPaidLicense = license.IsPaid;

            return lmAccount;
        }

        private async Task<LicenseInfo> EnsureHasCorrectLicense_NoSave(long lmAccountId, int userId, CommonContext db)
        {
            var now = DateTime.UtcNow;
            var paidLicense = await db.PaidLicenses
                .Where(x => x.user_id == userId
                    && !x.is_cancelled
                    && x.date_to > now)
                .OrderByDescending(x => x.date_to)
                .ThenByDescending(x => x.id)
                .Select(x => new
                {
                    x.id,
                    x.date_to,
                    x.user_id
                }).FirstOrDefaultAsync();

            var license = await db
                .LMAccountLicenses
                .Where(x => x.lm_account_id == lmAccountId)
                .FirstOrDefaultAsync();

            if (license == null)
            {
                var newLicense = new LMAccountLicense
                {
                    lm_account_id = lmAccountId,
                    date_from = now,
                    by_user_id = userId,
                    created = now,
                    modified = now,
                    date_to = paidLicense != null ? paidLicense.date_to : now.AddDays(_options.TrialLicenseLengthDays),
                    is_paid = paidLicense != null,
                    paid_license_id = paidLicense != null ? paidLicense.id : null
                };

                db.LMAccountLicenses.Add(newLicense);
                return new LicenseInfo
                {
                    DateFrom = newLicense.date_from,
                    DateTo = newLicense.date_to,
                    IsPaid = newLicense.is_paid
                };
            }
            else
            {
                if (paidLicense != null && (paidLicense.date_to - license.date_to).TotalSeconds > 1)
                {
                    license.date_to = paidLicense.date_to;
                    license.paid_license_id = paidLicense.id;
                    license.is_paid = true;
                    license.modified = now;
                    license.by_user_id = userId;
                    license.date_from = now;
                }

                return new LicenseInfo
                {
                    DateFrom = license.date_from,
                    DateTo = license.date_to,
                    IsPaid = license.is_paid
                };
            }
        }

        public (bool ok, string error) ValidateLmAccountInfo(LMAccountInfo lmAccount)
        {
            if (lmAccount == null)
            {
                return (false, "lm account is null");
            }
            if (string.IsNullOrEmpty(lmAccount.Token))
            {
                return (false, "token is empty");
            }
            if (lmAccount.LMAccountId <= 0)
            {
                return (false, "lm account id should be positive");
            }
            if (lmAccount.Id != 0)
            {
                return (false, "id should be 0");
            }
            return ValidateLmAccountSettings(lmAccount.Settings);
        }

        public (bool ok, string error) ValidateLmAccountSettings(LMAccountSettings settings)
        {
            if (settings?.AdditionalCurrencies != null)
            {
                if (settings.AdditionalCurrencies.Length > 100)
                {
                    return (false, "additional currencies count should be less than 100");
                }
                if (settings.AdditionalCurrencies.Any(x => x.Length != 3 || x.ToUpperInvariant().Any(y => y < 'A' || y > 'Z')))
                {
                    return (false, "additional currencies should be 3 letters");
                }
            }
            if (settings?.TransferCategoryId != null && settings.TransferCategoryId <= 0)
            {
                return (false, "transfer category id should be positive");
            }
            if (settings?.CrossCurrencyTransferCategoryId != null && settings.CrossCurrencyTransferCategoryId <= 0)
            {
                return (false, "cross currency transfer category id should be positive");
            }
            return (true, null);
        }

        public async Task<bool> UpdateLMAccountSettings(int userId, int id, LMAccountSettings settings)
        {
            using var db = _dbFactory.CreateDbContext();

            var additionalCurrenciesStr = string.Join(",", settings
                ?.AdditionalCurrencies
                ?.Select(x => x.ToUpperInvariant()) ?? Array.Empty<string>());

            int updated = await db.LMAccounts
                .Where(x => x.id == id && x.user_id == userId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.additional_currencies,
                    y => additionalCurrenciesStr)
                    .SetProperty(y => y.sort_tran_on_load_more, settings.SortTransactionOnLoadMore)
                    .SetProperty(y => y.transfer_category_id, settings.TransferCategoryId)
                    .SetProperty(y => y.cross_currency_transfer_category_id, settings.CrossCurrencyTransferCategoryId));

            return updated > 0;
        }

        public async Task<bool> ActivateLMAccount(int id, int onwerId)
        {
            using var db = _dbFactory.CreateDbContext();
            int updated = await db.LMAccounts
                .Where(x => x.user_id == onwerId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.is_active, x => x.id == id));
            return updated > 0;
        }

        public async Task<RequestSignature> LoadAuthInfoSignature(int userId)
        {
            using var db = _dbFactory.CreateDbContext();
            var lmAccountLicensDates = await (from a in db.LMAccounts
                                              join l in db.LMAccountLicenses
                                                 on a.lm_account_id equals l.lm_account_id
                                              where a.user_id == userId
                                              select l.date_to)
                                    .ToListAsync();

            var signature = _licenseCheckService.GetNewUnsigned();
            signature.Signature = _licenseCheckService.SignLowSecurity(
                userId,
                ServicesConstants.LicenseNameAuthInfo,
                signature.Timestamp,
                signature.Nonce,
                ServicesConstants.LicenseKeyFull,
                lmAccountLicensDates);

            return signature;
        }

        public async Task<bool> DeleteUserProfile(int userId)
        {
            using var db = _dbFactory.CreateDbContext();
            var user = await db.AppUsers
               .Where(x => x.id == userId)
               .FirstOrDefaultAsync();

            if (user == null || user.deleted.HasValue)
                return false;

            user.deleted = DateTime.Now;

            var accounts = await db.LMAccounts
                .Where(x => x.user_id == userId)
                .ToListAsync();

            foreach (var lmAccountId in accounts.Select(x => x.lm_account_id).Distinct())
            {
                await UpdateAccountLicenseOnRemove_NoSave(db, userId, lmAccountId);
            }

            await db.SaveChangesAsync();

            //delete lm accounts
            await db.LMAccounts.Where(x => x.user_id == userId)
                .ExecuteDeleteAsync();

            //delete ssoIds
            await db.UserSsos.Where(x => x.user_id == userId)
                .ExecuteDeleteAsync();

            return true;
        }

        public async Task<bool> DeleteSso(int userId, SsoIdInfo ssoId)
        {
            using var db = _dbFactory.CreateDbContext();

            //delete ssoIds
            var deleted = await db.UserSsos.Where(x => x.user_id == userId
                 && x.provider == ssoId.Provider
                  && x.sso_id == ssoId.SsoId)
                .ExecuteDeleteAsync();

            return deleted > 0;
        }

        public async Task<AuthUserInfo> GetUserAuthInfoUpdateActivity(int userId)
        {
            using var db = _dbFactory.CreateDbContext();
            var user = await db.AppUsers
                .FirstOrDefaultAsync(x => x.id == userId);

            if (user == null)
                return null;

            var accounts = await (from a in db.LMAccounts
                                  join l in db.LMAccountLicenses
                                     on a.lm_account_id equals l.lm_account_id
                                  where a.user_id == userId
                                  select new
                                  {
                                      id = a.id,
                                      is_active = a.is_active,
                                      lm_account_id = a.lm_account_id,
                                      name = a.name,
                                      token = a.token,
                                      user_name = a.user_name,
                                      additional_currencies = a.additional_currencies,
                                      transfer_category_id = a.transfer_category_id,
                                      cross_currency_transfer_category_id = a.cross_currency_transfer_category_id,
                                      sort_tran_on_load_more = a.sort_tran_on_load_more,
                                      license_date_to = l.date_to,
                                      license_is_paid = l.is_paid
                                  }).ToListAsync();

            var ssoIds = await db.UserSsos
                .Where(x => x.user_id == userId)
                .ToListAsync();

            var res = new AuthUserInfo
            {
                Id = user.id,
                Name = user.name,
                SsoAccounts = ssoIds.Select(x => new SsoInfo
                {
                    SsoId = x.sso_id,
                    Name = x.name,
                    Provider = x.provider,
                }).ToList(),
                Accounts = accounts.Select(x => new LMAccountInfo
                {
                    Id = x.id,
                    IsActive = x.is_active,
                    LMAccountId = x.lm_account_id,
                    Name = x.name,
                    Token = AesPasswordEncryptionHelper.Decrypt(x.token, GetLMAccountEncryptPassword(x.lm_account_id)),
                    UserName = x.user_name,
                    Settings = new LMAccountSettings
                    {
                        AdditionalCurrencies = x.additional_currencies?.Split(',').ToArray(),
                        SortTransactionOnLoadMore = x.sort_tran_on_load_more,
                        TransferCategoryId = x.transfer_category_id,
                        CrossCurrencyTransferCategoryId = x.cross_currency_transfer_category_id,
                    },
                    LicenseTill = x.license_date_to,
                    IsPaidLicense = x.license_is_paid
                }).ToList(),
                Signature = _licenseCheckService.GetNewUnsigned()
            };

            res.Signature.Signature = _licenseCheckService.SignLowSecurity(
                userId,
                ServicesConstants.LicenseNameAuthInfo,
                res.Signature.Timestamp,
                res.Signature.Nonce,
                ServicesConstants.LicenseKeyFull,
                res.Accounts.Select(x => x.LicenseTill));

            if (user.last_activity_date.AddHours(3) < DateTime.UtcNow)
            {
                user.last_activity_date = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return res;
        }

        public async Task<AuthUserInfo> LmTokenLogin(string token)
        {
            var lmUser = await LoadLmUser(token);
            if (lmUser == null)
                return null;

            var ssoId = new SsoId
            {
                Id = lmUser.UserId.ToString(),
                Name = lmUser.UserEmail,
                Provider = SharedConstants.LmSsoProviderName,
            };

            var userId = await GetUserIdBySsoId(ssoId);
            if (userId.HasValue)
            {
                return await GetUserAuthInfoUpdateActivity(userId.Value);
            }

            var newUserId = await CreateNewUserWithSso(ssoId);
            await AddLMAccount_NoActiveCheck(newUserId, new LMAccountInfo
            {
                LMAccountId = lmUser.AccountId,
                Name = lmUser.BudgetName,
                Token = token,
                Settings = new LMAccountSettings
                {
                    AdditionalCurrencies = null,
                    SortTransactionOnLoadMore = false,
                    TransferCategoryId = null
                },
                UserName = lmUser.UserName,
                IsActive = true,
            });
            return await GetUserAuthInfoUpdateActivity(newUserId);
        }

        private async Task<SsoId> UpdateSso(int userId, SsoId ssoId)
        {
            using var db = _dbFactory.CreateDbContext();

            var name = TextHelper.LimitLength(ssoId.Name, 250);
            var updated = await db.UserSsos.Where(x => x.provider == ssoId.Provider
            && x.sso_id == ssoId.Id)
                .ExecuteUpdateAsync(x =>
                    x.SetProperty(y => y.name, name)
                    .SetProperty(y => y.user_id, userId));
            if (updated > 0)
            {
                return new SsoId
                {
                    Name = name,
                    Provider = ssoId.Provider,
                    Id = ssoId.Id
                };
            }
            return null;
        }

        private async Task<bool> CheckUserHasOtherCredentials(int userId, SsoId ssoId)
        {
            using var db = _dbFactory.CreateDbContext();

            return await db.UserSsos.AnyAsync(
                x => x.user_id == userId
                    && (x.provider != ssoId.Provider
                    || x.sso_id != ssoId.Id));
        }

        public async Task<(SsoAddResult result, SsoId newSsoId)> LmTokenSsoAdd(int userId, string token)
        {
            var lmUser = await LoadLmUser(token);
            if (lmUser == null)
                return (SsoAddResult.Error, null);

            var ssoId = new SsoId
            {
                Id = lmUser.UserId.ToString(),
                Name = lmUser.UserEmail,
                Provider = SharedConstants.LmSsoProviderName,
            };
            return await AddSso(userId, ssoId);
        }

        public async Task<UserDto> LoadLmUser(string token)
        {
            var client = _httpFactory.CreateClient("LMAuth");

            var request = new HttpRequestMessage(HttpMethod.Get, "me");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var res = await response.Content.ReadFromJsonAsync<UserDto>();
            if (res?.BudgetName != null)
            {
                res.BudgetName = WebUtility.HtmlDecode(res.BudgetName);
            }
            return res;
        }
    }
}
