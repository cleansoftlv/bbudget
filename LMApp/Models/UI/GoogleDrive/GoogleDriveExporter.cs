using LMApp.Models.Configuration;
using LMApp.Models.Transactions;
using LMApp.Models.UI.GoogleDrive.Dto;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace LMApp.Models.UI.GoogleDrive
{
    public class GoogleDriveExporter(IJSRuntime js,
        IOptions<LocalOptions> options,
        FormatService formatService) : IAsyncDisposable
    {
        readonly IJSRuntime JS = js;

        private IJSObjectReference module;

        readonly LocalOptions options = options.Value;
        readonly FormatService formatService = formatService;


        public async ValueTask Init()
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import",
                $"./js/googleDriveExporter.js");
        }

        public async ValueTask<ExportCsvResult> ExportToGoogleDrive(ExportCsvRequest request)
        {
            try
            {
                request.OauthClientId = options.GoogleOAuthClientId;
                await EnsureInit();
                return await module.InvokeAsync<ExportCsvResult>("exportToGoogleDrive", request);
            }
            catch (JSException ex)
            {
                return new ExportCsvResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public IEnumerable<ExportTransaction> Convert(IEnumerable<TransactionDisplay> transactions)
        {
            return transactions.Select(t => new ExportTransaction
            {
                Date = t.Date.ToString("yyyy-MM-dd"),
                Account = t.AccountName,
                Payee = t.Payee,
                Amount = t.TranType == TransactionType.Transfer? 
                    (t.TransferBalanceAmount ?? 0) : 
                    t.Amount,
                TransferAmount = t.TranType == TransactionType.Transfer ?
                    t.Amount:
                    null,
                Category = t.CategoryName,
                Currency = t.Currency?.ToUpper(),
                Type = t.TranType.ToString(),
                Cleared = t.IsCleared,
            });
        }

        public async ValueTask EnsureInit()
        {
            if (module is null)
            {
                await Init();
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (module is not null)
            {
                await module.DisposeAsync();
            }
        }
    }
}
