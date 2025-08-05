using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Web;
using LMApp.Models.Account;
using LMApp.Models.Context;

namespace LMApp.Models.Transactions
{
    public class TransactionsService(
        IHttpClientFactory httpClientFactory,
        SettingsService settingsService)
    {

        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly SettingsService _settingsService = settingsService;

        public const char AssetAccountIdPrefix = 'a';
        public const char PlaidAccountIdPrefix = 'p';
        public const char TotalAccountIdPrefix = 't';
        public const char CryptoAccountIdPrefix = 'c';
        public const char OtherAccountIdPrefix = 'o';


        private HttpClient CreateHttpClient()
        {
            return _httpClientFactory.CreateClient("LM");
        }

        public async Task<GetTransactionsResult> GetTransactionsForCategoryAsync(
            long categoryId,
            int offset = 0,
            DateTime? endDate = null)
        {
            var end = endDate ?? DateTime.Now.Date.AddYears(1);

            return await GetTransactions(
                categoryId: categoryId,
                offset: offset > 0 ? offset : null,
                startDate: ClientConstants.MinDate,
                endDate: end);
        }

        public async Task<GetTransactionsResult> GetTransactionsForAssetOrPlaidAsync(
           string accountUid,
           int offset = 0,
           DateTime? startDate = null,
           DateTime? endDate = null)
        {
            var end = endDate ?? DateTime.Now.Date.AddYears(1);

            var parsed = ParseTranAccountUid(accountUid);

            return await GetTransactions(
                assetId: parsed.assetId,
                plaidAccountId: parsed.plaidAccountId,
                offset: offset > 0 ? offset : null,
                startDate: startDate ?? ClientConstants.MinDate,
                endDate: end);
        }

        public async Task<GetTransactionsResult> GetTransactionsForAccountAsync(
            long accountId,
            int offset = 0,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var end = endDate ?? DateTime.Now.Date.AddYears(1);

            return await GetTransactions(
                assetId: accountId,
                offset: offset > 0 ? offset : null,
                startDate: startDate ?? ClientConstants.MinDate,
                endDate: end);
        }

        public async Task<GetTransactionsResult> GetTransactionsForPlaidAccountAsync(
           long plaidAccountId,
           int offset = 0,
           DateTime? startDate = null,
           DateTime? endDate = null)
        {
            var end = endDate ?? DateTime.Now.Date.AddYears(1);

            return await GetTransactions(
                plaidAccountId: plaidAccountId,
                offset: offset > 0 ? offset : null,
                startDate: startDate ?? ClientConstants.MinDate,
                endDate: end);
        }

        public async Task<GetTransactionsResult> GetTransactionsForAccountAsync(
            long accountId,
            int offset,
            DateTime? startDate,
            DateTime? endDate,
            int limit)
        {
            var end = endDate ?? DateTime.Now.Date.AddYears(1);

            return await GetTransactions(
                assetId: accountId,
                offset: offset > 0 ? offset : null,
                startDate: startDate ?? ClientConstants.MinDate,
                endDate: end,
                limit: limit);
        }

        public async Task<GetTransactionsResult> GetAllTransactionsAsync(
            int offset = 0,
            DateTime? endDate = null,
            string status = null)
        {
            var end = endDate ?? DateTime.Now.Date.AddYears(1);

            return await GetTransactions(
                offset: offset > 0 ? offset : null,
                startDate: ClientConstants.MinDate,
                status: status,
                endDate: end);
        }

        public async Task<GetTransactionsResult> GetAllTransactionsAsync(
            DateTime startDate,
            DateTime endDate,
            int offset = 0,
            string status = null,
            int limit = ClientConstants.TransactionsPageSize)
        {
            return await GetTransactions(
                offset: offset > 0 ? offset : null,
                startDate: startDate,
                status: status,
                endDate: endDate,
                limit: limit);
        }



        private async Task<GetTransactionsResult> GetTransactions(
            long? tagId = null,
            long? recurringId = null,
            long? plaidAccountId = null,
            long? categoryId = null,
            long? assetId = null,
            bool? is_group = null,
            string status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? pending = null,
            int? offset = null,
            int? limit = null)
        {
            var sb = new StringBuilder();
            if (tagId.HasValue)
            {
                sb.Append($"tag_id={tagId.Value}");
            }
            if (recurringId.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"recurring_id={recurringId.Value}");
            }
            if (plaidAccountId.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"plaid_account_id={plaidAccountId.Value}");
            }
            if (categoryId.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"category_id={categoryId.Value}");
            }
            if (assetId.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"asset_id={assetId.Value}");
            }
            if (is_group.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"is_group={is_group.Value.ToString().ToLowerInvariant()}");
            }
            if (!string.IsNullOrEmpty(status))
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"status={HttpUtility.UrlEncode(status)}");
            }
            if (startDate.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"start_date={startDate.Value:yyyy-MM-dd}");
            }
            if (endDate.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"end_date={endDate.Value:yyyy-MM-dd}");
            }
            if (pending.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"pending={pending.Value.ToString().ToLowerInvariant()}");
            }
            if (offset.HasValue)
            {
                if (sb.Length > 0)
                    sb.Append("&");
                sb.Append($"offset={offset.Value}");
            }

            return await DoGetTransactionsForQueryAsync(sb.ToString(), maxItems: limit ?? ClientConstants.TransactionsPageSize);
        }



        public async Task UpdateTransaction(TransactionForEditDto tran, bool skipBalanceUpdate = false)
        {
            if (tran.id <= 0)
                throw new Exception("Invalid transaction id");

            var lmClient = CreateHttpClient();

            var url = $"transactions/{tran.id}";

            using var response =
                tran.plaid_account_id.HasValue ?
                 await lmClient.PutAsJsonAsync(url, new UpdatePlaidTransactionRequest
                 {
                     skip_balance_update = true,
                     transaction = tran.GetPlaidDto()
                 }) :
                await lmClient.PutAsJsonAsync(url, new UpdateTransactionRequest
                {
                    transaction = tran,
                    skip_balance_update = skipBalanceUpdate
                });

            response.EnsureSuccessStatusCode();

            var updateResponse = await response.Content.ReadFromJsonAsync<UpdateResponse>();
            if (updateResponse.error != null && updateResponse.error.Any())
            {
                throw new HttpRequestException($"Error updating transaction - {String.Join(", ", updateResponse.error)}",
                    null,
                    System.Net.HttpStatusCode.ExpectationFailed);
            }
        }


        public async Task<long[]> SplitTransaction(long tranId, Split[] splitParts)
        {
            if (tranId <= 0)
                throw new Exception("Invalid transaction id");

            var lmClient = CreateHttpClient();

            var request = new UpdateTransactionRequest
            {
                split = splitParts
            };


            using var response = await lmClient.PutAsJsonAsync($"transactions/{tranId}", request);

            response.EnsureSuccessStatusCode();

            var updateResponse = await response.Content.ReadFromJsonAsync<UpdateResponse>();
            if (updateResponse.error != null && updateResponse.error.Any())
            {
                throw new HttpRequestException($"Error splitting transaction - {String.Join(", ", updateResponse.error)}",
                   null,
                   System.Net.HttpStatusCode.ExpectationFailed);
            }

            return updateResponse.split;
        }

        public async Task<long[]> UnsplitTransaction(long tranId, bool andDelete = false)
        {
            if (tranId <= 0)
                throw new Exception("Invalid transaction id");

            var lmClient = CreateHttpClient();

            var request = new UnsplitRequest
            {
                parent_ids = [tranId],
                remove_parents = andDelete
            };

            using var response = await lmClient.PostAsJsonAsync($"transactions/unsplit", request);

            response.EnsureSuccessStatusCode();

            var strResponse = (await response.Content.ReadAsStringAsync()).Trim();
            if (strResponse.StartsWith('['))
            {
                var ids = JsonSerializer.Deserialize<long[]>(strResponse);
                if (!ids.Any())
                {
                    throw new HttpRequestException($"No transactions returned from unsplit",
                         null,
                         System.Net.HttpStatusCode.ExpectationFailed);
                }
                return ids;
            }
            else if (strResponse.StartsWith('{'))
            {
                var unsplitResponse = JsonSerializer.Deserialize<ResponseWithSingleError>(strResponse);
                if (!String.IsNullOrEmpty(unsplitResponse.error))
                {
                    throw new HttpRequestException($"Error unsplitting transaction - {String.Join(", ", unsplitResponse.error)}",
                       null,
                       System.Net.HttpStatusCode.ExpectationFailed);
                }
                else
                {
                    //Unknown response from unsplit
                    throw new HttpRequestException($"Unknown response from unsplit - {strResponse}",
                       null,
                       System.Net.HttpStatusCode.ExpectationFailed);
                }
            }
            else
            {
                throw new HttpRequestException($"Unknown response from unsplit - {strResponse}",
                      null,
                      System.Net.HttpStatusCode.ExpectationFailed);
            }
        }


        public async Task<long[]> DeleteTransactionGroup(long tranId)
        {
            if (tranId <= 0)
                throw new Exception("Invalid transaction id");

            var lmClient = CreateHttpClient();

            using var response = await lmClient.DeleteAsync($"transactions/group/{tranId}");
            response.EnsureSuccessStatusCode();
            var respData = await response.Content.ReadFromJsonAsync<DeleteGroupResponse>();
            if (respData.error != null && respData.error.Any())
            {
                throw new HttpRequestException($"Error deleting transaction. LM api errors: {String.Join(", ", respData.error)}", null, System.Net.HttpStatusCode.ExpectationFailed);
            }
            if (respData.transactions == null || !respData.transactions.Any())
            {
                throw new HttpRequestException("No transactions returned from delete group", null, System.Net.HttpStatusCode.ExpectationFailed);
            }
            return respData.transactions;
        }

        public async Task DeleteTransaction(long tranId)
        {
            if (tranId <= 0)
                throw new Exception("Invalid transaction id");

            var categoryId = _settingsService.GetCachedCategories()
                    .First().id;

            Split[] split = [new Split {
                amount = 0,
                category_id = categoryId,
                date = DateTime.Now.ToString("yyyy-MM-dd"),
                notes = "Deleted",
                payee = "Deleted"
            }];

            await UpdateTransaction(new TransactionForEditDto
            {
                id = tranId,
                amount = 0,
                category_id = categoryId,
                currency = _settingsService.PrimaryCurrency.ToLower(),
                date = DateTime.Now.ToString("yyyy-MM-dd"),
                notes = "Deleted",
                payee = "Deleted",
                status = ClientConstants.TransactionStatusCleared,
            });

            var ids = await SplitTransaction(tranId, split);
            await UnsplitTransaction(tranId, andDelete: true);
        }

        public async Task<long[]> InsertTransactions(TransactionForInsertDto[] transactions)
        {
            if (transactions == null || !transactions.Any())
                throw new Exception("transaction are empty");

            var lmClient = CreateHttpClient();

            var request = new InsertTransactionsRequest
            {
                transactions = transactions
            };

            using var response = await lmClient.PostAsJsonAsync($"transactions", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<InsertTransactionsResponse>();
            if (result.error != null && result.error.Any())
            {
                throw new HttpRequestException($"Error inserting transactions - {String.Join(", ", result.error)}", null, System.Net.HttpStatusCode.ExpectationFailed);
            }
            if (result.ids == null || !result.ids.Any())
            {
                throw new HttpRequestException("No transaction ids returned from insert", null, System.Net.HttpStatusCode.ExpectationFailed);
            }

            return result.ids;
        }

        public async Task<long> CreateGroup(CreateGroupDto group)
        {
            if (group?.transactions == null || !group.transactions.Any())
                throw new Exception("group is empty");

            var lmClient = CreateHttpClient();

            using var response = await lmClient.PostAsJsonAsync($"transactions/group", group);
            response.EnsureSuccessStatusCode();
            var resultStr = (await response.Content.ReadAsStringAsync()).Trim();
            if (long.TryParse(resultStr, out var result))
            {
                return result;
            }
            else if (resultStr.StartsWith('{'))
            {
                var respData = JsonSerializer.Deserialize<ResponseWithErrors>(resultStr);
                if (respData.error != null && respData.error.Any())
                {
                    throw new HttpRequestException($"Error creating group - {String.Join(", ", respData.error)}", null, System.Net.HttpStatusCode.ExpectationFailed);
                }
                else
                {
                    throw new HttpRequestException($"Unknown response from create group - {resultStr}", null, System.Net.HttpStatusCode.ExpectationFailed);
                }
            }
            else
            {
                throw new HttpRequestException($"Unknown response from create group - {resultStr}", null, System.Net.HttpStatusCode.ExpectationFailed);
            }
        }


        private async Task<GetTransactionsResult> DoGetTransactionsForQueryAsync(string query, int maxItems = ClientConstants.TransactionsPageSize)
        {
            var lmClient = CreateHttpClient();

            var response = await lmClient.GetFromJsonAsync<GetTransactionsResponse>(
                $"transactions?limit={maxItems}&{query}");

            if (response == null)
            {
                throw new HttpRequestException("No response from LM api",
                    null,
                    System.Net.HttpStatusCode.ExpectationFailed);
            }
            else if (!String.IsNullOrEmpty(response.error))
            {
                throw new HttpRequestException($"Error getting transactions - {String.Join(", ", response.error)}",
                     null,
                     System.Net.HttpStatusCode.BadRequest);
            }

            return new GetTransactionsResult
            {
                Transactions = response?.transactions
                    .OrderByDescending(x => x.date)
                    .ThenByDescending(x => x.id)
                    .Select(Convert)
                    .ToList(),
                HasMore = response.has_more
            };
        }


        public async Task<TransactionDisplay> GetTransactionAsync(long id)
        {
            var lmClient = CreateHttpClient();
            using var respMsg = await lmClient.GetAsync($"transactions/{id}");
            if (respMsg.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            var response = await respMsg.Content.ReadFromJsonAsync<TransactionDto>();
            if (response == null)
            {
                throw new HttpRequestException("No response from LM api", null, System.Net.HttpStatusCode.ExpectationFailed);
            }
            else if (response.GetErrors().Any())
            {
                if (response.GetErrors().First().StartsWith("Transaction ID not found"))
                {
                    return null;
                }

                throw new HttpRequestException($"Error getting transaction - {String.Join(", ", response.GetErrors())}", null, System.Net.HttpStatusCode.ExpectationFailed);
            }

            return Convert(response);
        }

        public async Task<TransactionDisplay> GetTransactionGroupAsync(long id)
        {
            var lmClient = CreateHttpClient();
            using var respMsg = await lmClient.GetAsync($"transactions/group?transaction_id={id}");
            if (respMsg.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            var response = await respMsg.Content.ReadFromJsonAsync<TransactionDto>();
            if (response == null)
            {
                throw new HttpRequestException("No response from LM api", null, System.Net.HttpStatusCode.ExpectationFailed);
            }
            else if (response.GetErrors().Any())
            {
                var firstError = response.GetErrors().First();
                if (firstError.EndsWith("is not a transaction group, or part of a transaction group.")
                    || firstError.StartsWith("Transaction ID not found"))
                {
                    return null;
                }
                throw new HttpRequestException($"Error getting transaction group - {String.Join(", ", response.GetErrors())}", null, System.Net.HttpStatusCode.ExpectationFailed);
            }

            return Convert(response);
        }


        public async Task<TransactionSplit> GetTransactionSplitAsync(
            long parentTranId,
            string accountUid,
            DateTime date)
        {
            var parsedUid = ParseTranAccountUid(accountUid);

            var tran = await GetTransactions(
                assetId: parsedUid.assetId,
                plaidAccountId: parsedUid.plaidAccountId,
                startDate: date,
                endDate: date.AddDays(1));

            var parent = tran.Transactions.FirstOrDefault(t => t.Id == parentTranId);
            var children = tran.Transactions
                .Where(t => t.ParentId == parentTranId)
                .OrderBy(t => t.Id)
                .ToList();

            if (parent == null)
            {
                parent = await GetTransactionAsync(parentTranId);
                if (parent.Date != date)
                {
                    var moreTrans = await GetTransactions(
                        assetId: parsedUid.assetId,
                        plaidAccountId: parsedUid.plaidAccountId,
                        startDate: parent.Date,
                        endDate: parent.Date.AddDays(1));

                    children.AddRange(moreTrans.Transactions
                        .Where(t => t.ParentId == parentTranId)
                        .OrderBy(t => t.Id)
                        .ToList());
                }
            }

            if (parent == null)
            {
                return null;
            }

            return new TransactionSplit
            {
                Parent = parent,
                Children = children
            };
        }

        public async Task InsertSplitTransaction(SplitTransactionForEdit split)
        {
            foreach (var item in split.Children)
            {
                item.AccountUid = split.AccountUid;
                item.Currency = split.Currency;
                item.Date = split.Date;
                item.Payee = split.Payee;
                item.IsCredit = split.IsCredit;
                item.Tags = split.Tags;
            }
            var lastChild = split.Children.Last();
            lastChild.Amount = split.LastChildAmount;
            lastChild.IsCredit = split.IsLastChildCredit;

            var insert = split.GetInsertDtos(_settingsService).First();
            var inserted = await InsertTransactions([insert]);
            split.Id = inserted.First();


            var splitParts = split.Children.Select(x => new Split
            {
                amount = (x.Amount ?? 0) * (x.IsCredit ? -1 : 1),
                category_id = x.CategoryId,
                date = x.Date.ToString("yyyy-MM-dd"),
                notes = x.Notes,
                payee = x.Payee
            }).ToArray();

            var ids = await SplitTransaction(split.Id, splitParts);

            if (ids.Length != split.Children.Count)
            {
                throw new HttpRequestException("Split failed, ids count returned from split does not match split parts count", null, System.Net.HttpStatusCode.ExpectationFailed);
            }

            for (int i = 0; i < split.Children.Count; i++)
            {
                split.Children[i].Id = ids[i];
            }
        }

        public async Task UpdateSplitTransaction(
            SplitTransactionForEdit split,
            TransactionType originalType)
        {
            PrepareSplitTransactionForSave(split);
            var resplitRequired = originalType != TransactionType.Split
                || split.Children.Count != split.OriginalChildTransactions.Length;

            if (resplitRequired)
            {
                if (originalType == TransactionType.Split)
                {
                    await UnsplitTransaction(split.Id);
                }
                var edit = split.GetUpdateDtos(_settingsService).First();
                var splitParts = split.Children.Select(x => new Split
                {
                    amount = x.AmountWithSign ?? 0,
                    category_id = x.CategoryId,
                    date = x.Date.ToString("yyyy-MM-dd"),
                    notes = x.Notes,
                    payee = x.Payee
                }).ToArray();

                await UpdateTransaction(edit);
                var splitResult = await SplitTransaction(edit.id, splitParts);
                if (splitResult.Length != split.Children.Count)
                {
                    throw new HttpRequestException("Split failed, ids count returned from split does not match split parts count", null, System.Net.HttpStatusCode.ExpectationFailed);
                }
                for (int i = 0; i < split.Children.Count; i++)
                {
                    split.Children[i].Id = splitResult[i];
                }
            }
            else
            {
                if (split.HasParentEditChanges)
                {
                    var edit = split.GetUpdateDtos(_settingsService).First();
                    await UpdateTransaction(edit);
                }

                foreach (var child in split.Children)
                {
                    if (child.HasEditChanges)
                    {
                        var edit = child.GetUpdateDtos(_settingsService).First();
                        await UpdateTransaction(edit, skipBalanceUpdate: true);
                    }
                }
            }
        }

        private static void PrepareSplitTransactionForSave(SplitTransactionForEdit split)
        {
            var usedOriginalChildIds = new HashSet<long>();

            foreach (var item in split.Children)
            {
                item.AccountUid = split.AccountUid;
                item.Currency = split.Currency;
                item.Date = split.Date;
                item.Payee = split.Payee;
                item.IsCredit = split.IsCredit;
                item.Tags = split.Tags;
                if (item.Id != 0)
                {
                    usedOriginalChildIds.Add(item.Id);
                }
            }
            var lastChild = split.Children.Last();
            lastChild.Amount = split.LastChildAmount;
            lastChild.IsCredit = split.IsLastChildCredit;


            var unusedOriginalChildIds = new Queue<TransactionDto>(
                (split.OriginalChildTransactions ?? Array.Empty<TransactionDto>())
                .Where(x => !usedOriginalChildIds.Contains(x.id)));

            foreach (var child in split.Children.Where(x => x.Id == 0))
            {
                if (!unusedOriginalChildIds.Any())
                    break;

                var unsued = unusedOriginalChildIds.Dequeue();
                child.Id = unsued.id;
                child.Transaction = unsued;
            }
        }

        public async ValueTask<BaseTransactionForEdit> GetForEdit(TransactionDisplay tran)
        {
            if (tran.TranType == TransactionType.Split
                || tran.TranType == TransactionType.SplitPart)
            {
                return await GetSplitForEdit(tran);
            }
            else if (tran.TranType == TransactionType.TransferPart)
            {
                var parentId = tran.GroupId.Value;
                var parent = await GetTransactionGroupAsync(parentId);
                if (parent == null)
                    return null;
                return parent.GetForEdit();
            }
            else
            {
                return tran.GetForEdit();
            }
        }

        private async ValueTask<BaseTransactionForEdit> GetSplitForEdit(TransactionDisplay tran)
        {
            var tranId = tran.ParentId ?? tran.Id;
            var accountUid = tran.Transaction.AccountUid;
            var date = tran.Date;

            var split = await GetTransactionSplitAsync(tranId, accountUid, date);
            if (split == null)
                return null;

            var splitforEdit = new SplitTransactionForEdit
            {
                Id = tranId,
                IsCleared = tran.IsCleared,
                Amount = Math.Abs(split.Parent.Amount),
                AccountUid = accountUid,
                Currency = split.Parent.Currency,
                Date = split.Parent.Date,
                IsCredit = split.Parent.Amount < 0,
                Notes = split.Parent.Notes,
                Payee = split.Parent.Payee,
                Tags = split.Parent.Transaction.tags?.Select(t => t.name).ToList(),
                Transaction = split.Parent.Transaction,
                OriginalChildTransactions = split.Children.Select(x => x.Transaction).ToArray(),
                Children = split.Children.Select(c => c.GetForEdit()).Cast<SimpleTransactionForEdit>().ToList()
            };

            return splitforEdit;
        }



        public TransactionDisplay Convert(TransactionDto dto)
        {
            var tranType = GetTransactionType(dto);

            var tran = new TransactionDisplay
            {
                Id = dto.id,
                IsCleared = dto.status == ClientConstants.TransactionStatusCleared,
                TranType = tranType,
                IsInsideGroup = dto.group_id != null,
                GroupId = dto.group_id,
                Amount = dto.amount,
                CategoryName = dto.category_name ?? "No category",
                CategoryId = dto.category_id,
                Currency = dto.currency,
                Payee = dto.payee,
                Notes = dto.notes,
                CreatedAt = dto.created_at,
                AccountName = dto.asset_name,
                AccountUid = GetAccountUid(dto.asset_id, dto.plaid_account_id),
                Date = DateTime.ParseExact(dto.date, "yyyy-MM-dd", null),
                Transaction = dto,
                ParentId = dto.parent_id,
            };

            switch (tranType)
            {
                case TransactionType.Simple:
                case TransactionType.TransferPart:
                    break;
                case TransactionType.Transfer:
                    {
                        tran.From = dto.children.FirstOrDefault(c => c.amount >= 0)
                             ?? dto.children.First();

                        tran.To = dto.children.FirstOrDefault(c => c.amount <= 0 && c != tran.From)
                            ?? dto.children.First(c => c != tran.From);

                        var sourceAccount = _settingsService.FindAssetOrPlaidAccount(tran.From?.asset_id, tran.From?.plaid_account_id);
                        var destinationAccount = _settingsService.FindAssetOrPlaidAccount(tran.To?.asset_id, tran.To?.plaid_account_id);

                        tran.AccountUid = sourceAccount?.AccountUid;
                        tran.AccountName = sourceAccount?.Name ?? "<Unknown>";

                        tran.DestinationAccountName = destinationAccount?.Name ?? "<Unknown>";
                      
                        if (tran.From != null)
                        {   
                            tran.Amount = tran.From.amount;
                            tran.Currency = tran.From.currency;
                        }
                        else if (tran.To != null)
                        {
                            tran.Amount = tran.To.amount;
                            tran.Currency = tran.To.currency;
                        }
                        tran.IsCrossCurrencyTransfer = dto.category_id == _settingsService.Settings.CrossCurrencyTransferCategoryId;
                        tran.TransferBalanceAmount = dto.amount;
                        tran.TransferBalanceCurrency = dto.currency;
                        break;
                    }
                case TransactionType.Split:
                    break;
                case TransactionType.Other:
                    break;
                case TransactionType.CategoryTransfer:
                    {
                        var sourceTransaction = dto.children.FirstOrDefault(c => c.amount >= 0)
                             ?? dto.children.First();
                        var destinationTransaction = dto.children.First(c => c.amount < 0 && c != sourceTransaction);

                        //var sourceCategory = accounts.FirstOrDefault(a => a.id == sourceTransaction.asset_id);
                        //var destinationAccount = accounts.FirstOrDefault(a => a.id == destinationTransaction.asset_id);

                        //tran.AccountName = sourceAccount?.name ?? "<Unknown>";
                        //tran.DestinationAccountName = destinationAccount?.name ?? "<Unknown>";

                        break;
                    }
                default:
                    break;
            }

            return tran;
        }

        public static int SortTransacactions(TransactionDisplay x, TransactionDisplay y)
        {
            var cmp = y.Transaction.date.CompareTo(x.Transaction.date);
            if (cmp != 0)
            {
                return cmp;
            }
            cmp = y.Transaction.created_at.CompareTo(x.Transaction.created_at);
            if (cmp != 0)
            {
                return cmp;
            }
            return y.Id.CompareTo(x.Id);
        }


        private TransactionType GetTransactionType(TransactionDto dto)
        {
            //Split
            if (!dto.is_group && dto.parent_id.HasValue)
            {
                if (dto.asset_id == null && dto.plaid_account_id == null)
                {
                    return TransactionType.Other;
                }
                else
                {
                    return TransactionType.SplitPart;
                }
            }

            //Split
            if (!dto.is_group && dto.has_children)
            {
                if (dto.asset_id == null && dto.plaid_account_id == null)
                {
                    return TransactionType.Other;
                }
                else
                {
                    return TransactionType.Split;
                }
            }

            //Transfer or other
            if (dto.group_id.HasValue)
            {
                return TransactionType.TransferPart;
            }

            if (!dto.is_group || dto.children == null || !dto.children.Any())
            {
                return TransactionType.Simple;
            }

            if ((dto.category_id == _settingsService.Settings.TransferCategoryId
                || (dto.category_id.HasValue && dto.category_id == _settingsService.Settings.CrossCurrencyTransferCategoryId))
                && dto.children.Length == 2)
            {
                return TransactionType.Transfer;
            }

            if (dto.children.Length != 2)
            {
                return TransactionType.Other;
            }
            else if (dto.children.All(x => x.amount == 0) || (dto.children.Any(x => x.amount < 0)
                && dto.children.Any(x => x.amount > 0)))
            {
                return TransactionType.Transfer;
            }
            else
            {
                return TransactionType.Other;
            }
        }


        public static string GetAccountUid(long? assetId, long? plaidAccountId)
        {
            if (assetId.HasValue)
            {
                return $"{AssetAccountIdPrefix}{assetId.Value}";
            }
            else if (plaidAccountId.HasValue)
            {
                return $"{PlaidAccountIdPrefix}{plaidAccountId.Value}";
            }
            else
            {
                return null;
            }
        }

        public static AccountType? GetAccountTypeByUid(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return null;
            }
            else if (uid.StartsWith(AssetAccountIdPrefix))
            {
                return AccountType.Default;
            }
            else if (uid.StartsWith(PlaidAccountIdPrefix))
            {
                return AccountType.Plaid;
            }
            else
            {
                throw new Exception($"Invalid account uid: {uid}");
            }
        }

        public static (long? assetId, long? plaidAccountId) ParseTranAccountUid(string uid)
        {
            if (uid.StartsWith(AssetAccountIdPrefix))
            {
                var idStr = uid.AsSpan(1);
                return (long.Parse(idStr), null);
            }
            else if (uid.StartsWith(PlaidAccountIdPrefix))
            {
                var idStr = uid.AsSpan(1);
                return (null, long.Parse(idStr));
            }
            else if (string.IsNullOrEmpty(uid))
            {
                return default;
            }
            else
            {
                throw new Exception($"Invalid account uid: {uid}");
            }
        }

        public static bool AccountTypeCanBeUsedInTransaction(AccountType type) => type == AccountType.Default || type == AccountType.Plaid;
        public static bool AccountTypeCanBeUsedInTransaction(AccountType? type) => type.HasValue && AccountTypeCanBeUsedInTransaction(type.Value);

    }
}