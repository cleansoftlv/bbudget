using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models
{
    public static class ClientConstants
    {
        public const string AppName = "BBudget";
        public const int TransactionsPageSize = 100; 
        public const int ReportsTransactionsPageSize = 1000; // Use max limit for reports
        public const int InfiniteScrollLoadDelay = 100;
        public const int OverlapOffsetForTransactions = 5;
        public const string XClientAppHeaderValue = Shared.SharedConstants.XClientAppHeaderValue;
        public const string XClientAppHeaderKey = Shared.SharedConstants.XClientAppHeaderKey;

        public readonly static DateTime MinDate = new DateTime(2000, 1, 1);

        public const string TransactionStatusCleared = "cleared";
        public const string TransactionStatusUncleared = "uncleared";

        public const string DateFormat = "dd.MM.yyyy";
        public const string ClientNameKey = "ClientName";


    }
}
