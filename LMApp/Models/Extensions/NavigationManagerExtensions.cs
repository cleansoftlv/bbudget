using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;

namespace LMApp.Models.Extensions
{
    public static class NavigationManagerExtensions

    {
        public static bool UriStartsWith(this NavigationManager navigationManager,
         string uri)
        {
            return navigationManager.ToBaseRelativePath(navigationManager.Uri).StartsWith(uri);
        }

        public static bool RelativeUriEqualsFull(this NavigationManager navManager, string uriToMatch)
        {
            var uri = navManager.ToAbsoluteUri(navManager.Uri);

            return uri.PathAndQuery.Equals(uriToMatch, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool RelativeUriPathEquals(this NavigationManager navManager, string uriToMatch)
        {
            var uri = navManager.ToAbsoluteUri(navManager.Uri);

            if ((uriToMatch == "/" && uri.AbsolutePath == "")
                || ( uriToMatch == "" && uri.AbsolutePath == "/"))
                return true;

            return uri.AbsolutePath.TrimStart('/').Equals(uriToMatch, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool TryGetQueryString<T>(this NavigationManager navManager, string key, out T value)
        {
            var uri = navManager.ToAbsoluteUri(navManager.Uri);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue(key, out var valueFromQueryString))
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                switch (Type.GetTypeCode(targetType))
                {
                    case TypeCode.Int32:
                        if (int.TryParse(valueFromQueryString, CultureInfo.InvariantCulture, out var valueAsInt))
                        {
                            value = (T)(object)valueAsInt;
                            return true;
                        }
                        break;
                    case TypeCode.Int64:
                        if (long.TryParse(valueFromQueryString, CultureInfo.InvariantCulture, out var valueAsLong))
                        {
                            value = (T)(object)valueAsLong;
                            return true;
                        }
                        break;
                    case TypeCode.Boolean:
                        if (bool.TryParse(valueFromQueryString, out var valueAsBool))
                        {
                            value = (T)(object)valueAsBool;
                            return true;
                        }
                        break;
                    case TypeCode.String:
                        value = (T)(object)valueFromQueryString.ToString();
                        return true;
                    case TypeCode.Decimal:
                        if (decimal.TryParse(valueFromQueryString, CultureInfo.InvariantCulture, out var valueAsDecimal))
                        {
                            value = (T)(object)valueAsDecimal;
                            return true;
                        }
                        break;
                }
            }

            value = default;
            return false;
        }

        public static T GetQueryStringOrDefault<T>(this NavigationManager navManager, string key)
        {
            if (TryGetQueryString(navManager, key, out T val))
            {
                return val;
            }
            return default;
        }

    }
}