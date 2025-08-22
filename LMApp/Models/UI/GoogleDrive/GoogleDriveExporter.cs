using LMApp.Models.Configuration;
using LMApp.Models.Reports;
using LMApp.Models.Transactions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace LMApp.Models.UI.GoogleDrive
{
    public class GoogleDriveExporter(IJSRuntime js,
        IOptions<LocalOptions> options,
        ReportsService reportService,
        NavigationManager navigationManager,
        UserContextService userContextService
        ) : IAsyncDisposable
    {
        readonly IJSRuntime JS = js;

        private IJSObjectReference module;

        readonly LocalOptions options = options.Value;
        readonly ReportsService _reportService = reportService;
        readonly NavigationManager _navigationManager = navigationManager;
        readonly UserContextService _userContextService = userContextService;


        public async ValueTask Init()
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import",
                $"./js/googleDriveExporter.js");
        }

        public async ValueTask<ExportCsvResult> ExportTransactionsToGoogleDrive(ExportTransactionsCsvRequest request)
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

        public async ValueTask<ExportCsvResult> ExportRawCsvToGoogleDrive(string csvContent, string fileName)
        {
            try
            {
                await EnsureInit();
                
                var request = new
                {
                    CsvContent = csvContent,
                    FileName = fileName,
                    OauthClientId = options.GoogleOAuthClientId
                };
                
                return await module.InvokeAsync<ExportCsvResult>("exportRawCsvToGoogleDrive", request);
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
            var lmAccountId = _userContextService.CurrentAccount.AccountId;
            var origin = _navigationManager.BaseUri.TrimEnd('/');

            return transactions.Select(x => _reportService.ConvertTranToExportFormat(x, lmAccountId, origin));
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
