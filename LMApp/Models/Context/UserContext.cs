using Shared.LMApi;
using System.Net;

namespace LMApp.Models.Account
{
    public class UserContext
    {
        public UserContext(UserDto user, string token)
        {
            UserName = user.UserName;
            BudgetName = WebUtility.HtmlDecode(user.BudgetName);
            ApiKeyName = user.ApiKeyLabel;
            AccountId = user.AccountId;
            PrimaryCurrency = user.PrimaryCurrency.ToUpperInvariant();
            ApiToken = token;
        }
        public UserContext()
        {
        }
        public int Id { get; set; }
        public long AccountId { get; set; }
        public string UserName { get; set; }
        public string BudgetName { get; set; }
        public string ApiKeyName { get; set; }
        public string ApiToken { get; set; }

        public string PrimaryCurrency { get; set; }

        public bool IsActive { get; set; }

        public DateTime LicenseExpirationDate { get; set; }
    }
}