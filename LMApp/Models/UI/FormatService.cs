using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Shared;
using Shared.Helpers;
using Shared.License;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;

namespace LMApp.Models.UI
{
    public class FormatService
    {
        public MarkupString FormatBalance(decimal amount, string currency, bool isLiability = false)
        {
            var sb = new StringBuilder();
            bool needClose = false;
            if (amount < 0)
            {
                needClose = true;
                if (isLiability)
                {
                    sb.Append('-');
                }
                else
                {
                    sb.Append("<span class='amount-neg'>-");
                }
            }
            else if (amount > 0 && isLiability)
            {
                needClose = true;
                sb.Append("<span class='amount-credit'>+");
            }

            string amountStr = FormatAmountDecimals(amount);
            sb.Append(amountStr);
            sb.Append(" ");
            sb.Append(CurrencyToSymbol(currency));

            if (needClose)
            {
                sb.Append("</span>");
            }

            return new MarkupString(sb.ToString());
        }

        public string FormatDate(DateTime date)
        {
            return date.ToString(ClientConstants.DateFormat);
        }

        public string FormatMonth(DateTime date)
        {
            if (date.Year == DateTime.Now.Year)
            {
                return date.ToString(ClientConstants.MonthFormatNoYear);
            }

            return date.ToString(ClientConstants.MonthFormatWithYear);
        }

        public static string LimitLength(string s, int maxLength, string trimToken = "…")
        {
            return TextHelper.LimitLength(s, maxLength, trimToken);
        }

        public MarkupString FormatTranAmount(decimal amount, string currency)
        {
            var sb = new StringBuilder();
            if (amount < 0)
            {
                sb.Append("<span class='amount-credit'>+");
            }

            string amountStr = FormatAmountDecimals(amount);

            sb.Append($"{amountStr} {CurrencyToSymbol(currency)}");

            if (amount < 0)
            {
                sb.Append("</span>");
            }

            return new MarkupString(sb.ToString());
        }

        //Format price
        public string FormatPrice(decimal amount, string currency)
        {
            return $"{FormatAmountDecimals(amount)}{CurrencyToSymbol(currency)}";
        }

        public string FormatProductInterval(int days)
        {
            return LicenseCheckService.FormatProductInterval(days);
        }

        public static RenderFragment SelectItemTemplateWithEmpty(SelectedItem item)
        {
            return builder =>
            {
                if (String.IsNullOrEmpty(item.Value))
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "empty-item");
                    builder.AddContent(2, "Empty");
                    builder.CloseElement();
                }
                else
                {
                    builder.AddContent(0, item.Text);
                }
            };
        }

        private static string FormatAmountDecimals(decimal amount)
        {
            return Math.Abs(amount).ToString(amount % 1 == 0 ? "F0" : "F2");
        }


        public string AuthProviderName(string provider)
        {
            return provider switch
            {
                "sw_aad" => "Microsoft Account",
                "sw_github" => "Github Account",
                SharedConstants.LmSsoProviderName => "Lunch Money Account",
                _ => provider,
            };
        }

        public DateTime ConvertFromUtcToLocal(DateTime date)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(date, TimeZoneInfo.Local);
        }

        public string CurrencyToSymbol(string currencyIso)
        {
            if (currencyIso == null)
                return "?";

            switch (currencyIso.ToUpperInvariant())
            {
                case "USD":
                    return "$";
                case "EUR":
                    return "€";
                case "GBP":
                    return "£";
                case "JPY":
                    return "¥";
                case "CNY":
                    return "¥";
                case "INR":
                    return "₹";
                case "CAD":
                    return "CA$";
                case "AUD":
                    return "AU$";
                case "NZD":
                    return "NZ$";
                case "CHF":
                    return "CHF";
                case "ZAR":
                    return "R";
                case "BRL":
                    return "R$";
                case "RUB":
                    return "₽";
                case "TRY":
                    return "₺";
                case "KRW":
                    return "₩";
                case "SEK":
                    return "kr";
                case "NOK":
                    return "kr";
                case "DKK":
                    return "kr";
                case "SGD":
                    return "S$";
                case "HKD":
                    return "HK$";
                case "TWD":
                    return "NT$";
                case "MXN":
                    return "Mex$";
                case "PHP":
                    return "₱";
                case "IDR":
                    return "Rp";
                case "MYR":
                    return "RM";
                case "THB":
                    return "฿";
                case "VND":
                    return "₫";
                case "CZK":
                    return "Kč";
                case "HUF":
                    return "Ft";
                case "PLN":
                    return "zł";
                case "ILS":
                    return "₪";
                case "SAR":
                    return "﷼";
                case "AED":
                    return "د.إ";
                case "CLP":
                    return "CLP$";
                case "KWD":
                    return "د.ك";
                case "QAR":
                    return "﷼";
                case "COP":
                    return "COL$";
                case "PEN":
                    return "S/.";
                case "RON":
                    return "lei";
                default:
                    return currencyIso;
            }
        }
    }
}
