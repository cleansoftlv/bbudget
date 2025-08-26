using BootstrapBlazor.Components;
using LMApp.Models.Context;
using LMApp.Models.Extensions;
using LMApp.Models.Transactions;
using LMApp.Models.UI;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace LMApp.Pages
{
    public abstract class BaseTransactionPage : BasePage
    {


        [Inject]
        public TransactionsService transactionsService { get; set; }

        [Inject]
        public SettingsService settingsService { get; set; }

        [SupplyParameterFromQuery(Name = "tid")]
        public long? UrlTransactionId { get; set; }






        protected int UnfilteredTransactionCount;
        protected List<TransactionDisplay> Transactions;
        protected string LoadTransactionsError;
        protected string LoadSingleTransactionError;
        protected string SaveTransactionError;
        protected string LoadMoreTransactionsError;
        protected bool HasMoreTrans;
        protected bool LoadingMoreTrans;
        protected BaseTransactionForEdit TransactionInEdit;
        protected long? CurrentDisplayTranEditId;
        protected TransactionDisplay SelectedTransaction;
        protected bool IsSaving;
        protected bool ShowTranForm;
        protected abstract void RefreshActivePage();
        protected abstract Task LoadMoreTransactions();

        protected override async Task OnParametersSetAsync()
        {
            if (LoadCancelled)
                return;


            if (UrlTransactionId.HasValue)
            {
                if (UrlTransactionId == 0 && SelectedTransaction == null)
                {
                    await ShowNewTransaction();
                }
                else if (SelectedTransaction?.Id != UrlTransactionId)
                {
                    var transaction = Transactions?.FirstOrDefault(x => x.Id == UrlTransactionId);
                    if (transaction != null)
                    {
                        await ShowTransaction(transaction);
                    }
                    else
                    {
                        var tran = await transactionsService.GetTransactionAsync(UrlTransactionId.Value);
                        if (tran != null)
                        {
                            await ShowTransaction(tran);
                        }
                    }
                }
            }
            else if (ShowTranForm)
            {
                DoCloseTranForm();
            }

            await base.OnParametersSetAsync();
        }


        protected async Task DeleteTransaction(BaseTransactionForEdit tran)
        {
            try
            {
                ClearSelectedForCreateTransfer(tran.Id);
                await DoDeleteTransaction(tran);
            }
            catch (HttpRequestException e)
            {
                e.LogIfRequired(log);
                SaveTransactionError = e.GetDescriptionForUser();
                IsSaving = false;
            }
        }
        private async Task DoDeleteTransaction(BaseTransactionForEdit tran)
        {
            IsSaving = true;
            SaveTransactionError = null;

            var childTrans = tran.ChildTransactionIds;
            if (tran.TranType == TransactionType.Split)
            {
                var split = (SplitTransactionForEdit)tran;
                split.Amount = 0;
                var update = split.GetUpdateDto();
                await transactionsService.UpdateTransaction(update);
                await transactionsService.UnsplitTransaction(split.Id, andDelete: true);
            }
            else
            {
                if (!settingsService.GetCachedCategories().Any())
                {
                    await CloseTranForm();
                    await ModalContainer.Modal.Show(new InfoModalVM
                    {
                        Title = "Delete error",
                        Message = new MarkupString("Due to Lunch Money API limitations, it is not possible to delete a transaction unless you add at least one category. Please add one category in LM and try again."),
                        AdditionalButtonText = "Go to LM settings",
                        AdditionalButtonColor = Color.Primary,
                        AdditionalButtonCallback = async () =>
                        {
                            await utils.OpenNewTab("https://my.lunchmoney.app/categories");
                            await ModalContainer.Modal.Hide();
                        }
                    });
                    return;
                }

                if (tran.GroupTransactionId != null)
                {
                    await transactionsService.DeleteTransactionGroup(tran.GroupTransactionId.Value);
                }
                foreach (var id in childTrans)
                {
                    await transactionsService.DeleteTransaction(id.Id);
                }
                if (tran.GroupTransactionId == null)
                {
                    await transactionsService.DeleteTransaction(tran.Id);
                }
            }
            var ids = childTrans.Select(x => x.Id).ToArray();
            int removed = Transactions.RemoveAll(x => ids.Contains(x.Transaction.id) || x.Transaction.id == tran.Id);
            UnfilteredTransactionCount -= removed;
            await TransactionDeleted(tran);
            await CloseTranForm();
        }

        public async Task ShowNewTransaction(bool force = false)
        {
            if (SelectedTransaction?.Id == 0 && !force)
                return;

            if (CurrentDisplayTranEditId == 0)
                return;

            SelectedTransaction = new TransactionDisplay
            {
                Id = 0
            };

            var tran = new SimpleTransactionForEdit
            {
                Date = DateTime.Now,
                Currency = settingsService.PrimaryCurrency,
                Id = 0,
                IsCleared = true,
                IsCredit = false,
            };

            PrepareNewTransaction(tran);

            TransactionInEdit = tran;
            CurrentDisplayTranEditId = 0;

            bool startTransition = !ShowTranForm;
            if (startTransition)
            {
                await StartTransition(BreakPoint.Large);
            }
            ShowTranForm = true;
            RefreshActivePage();
            if (startTransition)
            {
                await EndTransition();
            }
        }

        public async Task CopyTransaction(BaseTransactionForEdit tran)
        {
            SelectedTransaction = new TransactionDisplay
            {
                Id = 0
            };

            TransactionInEdit = tran.CopyForNew(settingsService);
            CurrentDisplayTranEditId = 0;
            if (!ShowTranForm)
            {
                await StartTransition(BreakPoint.Large);
                ShowTranForm = true;
                RefreshActivePage();
                await EndTransition();
            }
        }

        protected abstract void PrepareNewTransaction(SimpleTransactionForEdit tran);

        protected abstract Task AddNewSplitTransaction(SplitTransactionForEdit tran);


        protected void AddNewTransaction(TransactionDisplay tran, BaseTransactionForEdit update)
        {
            if (Transactions == null)
                return;

            if (ShouldAddTransactionToList(tran, update))
            {
                if (tran.TranType == TransactionType.Split
                    || tran.TranType == TransactionType.Transfer)
                {
                    tran.Context = TransactionListContext.None;
                }

                UnfilteredTransactionCount++;
                Transactions.Add(tran);
                SelectedTransaction = tran;
                Transactions.Sort(TransactionsService.SortTransacactions);
            }
        }

        protected abstract bool ShouldAddTransactionToList(
            TransactionDisplay tran,
            BaseTransactionForEdit update);

        protected async Task<TransactionDisplay> TryRefreshDisplayTransaction(long id, BaseTransactionForEdit update, long? newId = null)
        {
            var index = Transactions.FindIndex(0, x => x.Transaction.id == id);
            if (index >= 0)
            {
                var updatedTran = await transactionsService.GetTransactionAsync(newId ?? id);
                if (ShouldAddTransactionToList(updatedTran, update))
                {
                    if (updatedTran.TranType == TransactionType.Split
                        || updatedTran.TranType == TransactionType.Transfer)
                    {
                        updatedTran.Context = TransactionListContext.None;
                    }

                    Transactions[index] = updatedTran;
                    Transactions.Sort(TransactionsService.SortTransacactions);
                }
                else
                {
                    Transactions.RemoveAt(index);
                    UnfilteredTransactionCount--;
                }
                return updatedTran;
            }

            return null;
        }

        protected void TryRemoveDisplyTransaction(long id)
        {
            var index = Transactions.FindIndex(0, x => x.Transaction.id == id);
            if (index >= 0)
            {
                Transactions.RemoveAt(index);
                UnfilteredTransactionCount--;
            }
        }

        protected virtual async Task CloseTranForm()
        {
            bool startTransition = ShowTranForm;

            if (startTransition)
                await StartTransition(BreakPoint.Large);
            DoCloseTranForm();
            if (startTransition)
                await EndTransition();

            await ResponsiveNavigate(navigationManager.GetUriWithQueryParameter("tid", (long?)null),
                NavDirection.Back);
        }

        protected void DoCloseTranForm()
        {
            if (!ShowTranForm)
            {
                return;
            }

            IsSaving = false;
            //SelectedTransaction = null;
            TransactionInEdit = null;
            CurrentDisplayTranEditId = null;
            ShowTranForm = false;
            RefreshActivePage();
        }

        protected async Task NavigateToTransaction(TransactionDisplay tran)
        {
            var task1 = ShowTransaction(tran, force: true);
            var task2 = ResponsiveNavigate(navigationManager.GetUriWithQueryParameter("tid", tran.Id),
                NavDirection.Forward);
            await Task.WhenAll(task1.AsTask(), task2.AsTask());
        }

        protected async Task NavigateToNewTransaction()
        {
            var task1 = ShowNewTransaction(force: true);
            var task2 = ResponsiveNavigate(navigationManager.GetUriWithQueryParameter("tid", 0L),
                NavDirection.Forward);
            await Task.WhenAll(task1, task2.AsTask());
        }

        protected async ValueTask ShowTransaction(TransactionDisplay tran, bool force = false)
        {
            if (SelectedTransaction?.Id == tran.Id && !force)
            {
                return;
            }

            if (CurrentDisplayTranEditId == tran.Id)
            {
                return;
            }

            LoadSingleTransactionError = null;
            SelectedTransaction = tran;
            bool startTransition = !ShowTranForm;
            if (startTransition)
                await StartTransition(BreakPoint.Large);
            TransactionInEdit = null;
            ShowTranForm = true;
            if (startTransition)
                await EndTransition();

            try
            {
                var loaded = await transactionsService.GetForEdit(tran);
                if (loaded == null)
                {
                    if (SelectedTransaction != tran || !ShowTranForm)
                    {
                        return;
                    }
                    LoadSingleTransactionError = "Failed to load transaction. This can happen when split transactions contain nested transactions with different dates. To fix this, you may edit this transaction in Lunch Money.";
                    return;
                }

                TransactionInEdit = loaded;
                CurrentDisplayTranEditId = tran.Id;
                if (SelectedTransaction != tran || !ShowTranForm)
                {
                    return;
                }
            }
            catch (HttpRequestException e)
            {
                if (SelectedTransaction != tran || !ShowTranForm)
                {
                    return;
                }
                e.LogIfRequired(log);
                LoadSingleTransactionError = e.GetDescriptionForUser();
                return;
            }

            ShowTranForm = TransactionInEdit != null;
            RefreshActivePage();
        }

        protected abstract Task<bool> TransactionDeleted(BaseTransactionForEdit updated);
        protected abstract Task<bool> TransactionAdded(BaseTransactionForEdit updated);

        protected async Task SaveTransactionAndNext(ForEditPair tran)
        {
            var index = Transactions?.FindIndex(x => x.Id == tran.Original?.Id) ?? -1;
            var next = index == -1 || (index + 1 == Transactions.Count)
                ? null :
                Transactions[index + 1];
            bool saved = await SaveTransaction(tran, keepOpen: true);
            if (!saved)
            {
                return;
            }
            if (tran.Original?.Id != 0)
            {
                if (next != null)
                {
                    next = Transactions.FirstOrDefault(x => x.Id == next.Id);
                }
                if (next == null)
                {
                    await CloseTranForm();
                    return;
                }
                await ShowTransaction(next);
            }
            else
            {
                tran.Updated.ClearAmount();
                await CopyTransaction(tran.Updated);
            }
        }

        protected async Task<bool> CreateTransfer(ForEditPair pair)
        {
            try
            {
                await DoCreateTransfer(pair);
                userService.TransactionForCreateTransfer = null;
                return true;
            }
            catch (HttpRequestException e)
            {
                e.LogIfRequired(log);
                SaveTransactionError = e.GetDescriptionForUser();
                IsSaving = false;
                return false;
            }
        }

        protected async Task UngroupWithConfirm(AccountTransferTransactionForEdit transfer)
        {
            await ModalContainer.Modal.Show(new InfoModalVM
            {
                Title = "Ungroup transfer",
                Message = new MarkupString("Are you sure you want to ungroup this transfer? It will be split into two separate transactions."),
                AdditionalButtonText = "Ungroup",
                AdditionalButtonColor = Color.Primary,
                AdditionalButtonCallback = async () =>
                {
                    await ModalContainer.Modal.Hide();
                    await Ungroup(transfer);
                }
            });
        }

        protected async Task<bool> Ungroup(AccountTransferTransactionForEdit transfer)
        {
            try
            {
                await DoUngroup(transfer);
                return true;
            }
            catch (HttpRequestException e)
            {
                e.LogIfRequired(log);
                SaveTransactionError = e.GetDescriptionForUser();
                IsSaving = false;
                return false;
            }
        }


        protected async Task<bool> SaveTransaction(ForEditPair pair, bool keepOpen = false)
        {
            try
            {
                ClearSelectedForCreateTransfer(pair.Updated.Id);
                return await DoSaveTransaction(pair, keepOpen);
            }
            catch (HttpRequestException e)
            {
                e.LogIfRequired(log);
                SaveTransactionError = e.GetDescriptionForUser();
                IsSaving = false;
                return false;
            }
        }

        private void ClearSelectedForCreateTransfer(long editedId)
        {
            if (userService.TransactionForCreateTransfer != null
                && userService.TransactionForCreateTransfer.Id == editedId)
            {
                userService.TransactionForCreateTransfer = null;
            }
        }

        private async Task DoCreateTransfer(ForEditPair pair)
        {
            SaveTransactionError = null;
            IsSaving = true;
            StateHasChanged();

            var from = pair.Original as SimpleTransactionForEdit;
            var to = pair.Updated as SimpleTransactionForEdit;
            var transfer = AccountTransferTransactionForEdit.CreateFromTwo(from, to);
            var inserts = transfer.GetInsertDtos(settingsService);
            await transactionsService.UpdateTransaction(inserts[0].GetUpdate(from.Id));
            await transactionsService.UpdateTransaction(inserts[1].GetUpdate(to.Id));
            var group = transfer.GetGroupDto(settingsService, [from.Id, to.Id]);
            long groupId = await transactionsService.CreateGroup(group);
            var newTran = await transactionsService.GetTransactionAsync(groupId);
            TryRemoveDisplyTransaction(from.Id);
            TryRemoveDisplyTransaction(to.Id);
            AddNewTransaction(newTran, transfer);
            transfer.Id = groupId;
            transfer.SavedTransaction = newTran.Transaction;
            transfer.SavedFrom = newTran.From;
            transfer.SavedTo = newTran.To;

            if (await TransactionDeleted(from))
            {
                if (await TransactionDeleted(to))
                {
                    await TransactionAdded(transfer);
                }
            }

            await CloseTranForm();
            IsSaving = false;
        }

        private async Task DoUngroup(AccountTransferTransactionForEdit transfer)
        {
            SaveTransactionError = null;
            IsSaving = true;
            StateHasChanged();

            var ids = await transactionsService.DeleteTransactionGroup(transfer.Id);

            var newForEdit = new List<BaseTransactionForEdit>();
            foreach (var id in ids)
            {
                var updated = await TryRefreshDisplayTransaction(id, transfer, id);
                if (updated == null)
                {
                    updated = await transactionsService.GetTransactionAsync(id);
                    var forEdit = updated.GetForEdit();
                    AddNewTransaction(updated, forEdit);
                    newForEdit.Add(forEdit);
                }
            }

            TryRemoveDisplyTransaction(transfer.Id);

            if (await TransactionDeleted(transfer))
            {
                foreach (var update in newForEdit)
                {
                    if (!await TransactionAdded(update))
                        break;
                }
            }

            await CloseTranForm();
            IsSaving = false;
        }

        private async Task<bool> DoSaveTransaction(ForEditPair pair, bool keepOpen)
        {
            SaveTransactionError = null;
            var tran = pair.Updated;

            IsSaving = true;
            StateHasChanged();

            if (tran.Id != 0)
            {
                if (pair.Original.TranType == tran.TranType)
                {
                    await UpdateTranSameType(tran);
                }
                else
                {
                    if (!await UpdateTranAfterTypeChange(pair))
                    {
                        IsSaving = false;
                        return false;
                    }
                }

                await TranUpdated(pair);
            }
            else
            {
                if (tran.TranType == TransactionType.Split)
                {
                    var split = tran as SplitTransactionForEdit;
                    await transactionsService.InsertSplitTransaction(split);
                    await AddNewSplitTransaction(split);
                }
                else
                {
                    var inserts = tran.GetInsertDtos(settingsService);

                    var ids = await transactionsService.InsertTransactions(inserts);
                    var group = tran.GetGroupDto(settingsService, ids);
                    if (group != null)
                    {
                        long groupId = await transactionsService.CreateGroup(group);
                        var tranToAddId = groupId;
                        if (CurrentAccountUid != null)
                        {
                            for (int i = 0; i < inserts.Length; i++)
                            {
                                if (CurrentAccountUid == TransactionsService.GetAccountUid(inserts[i].asset_id, inserts[i].plaid_account_id))
                                {
                                    tranToAddId = ids[i];
                                    break;
                                }
                            }
                        }
                        await HandleNewTranCreated(tran, tranToAddId);
                    }
                    else
                    {
                        foreach (var id in ids)
                        {
                            var newTran = await transactionsService.GetTransactionAsync(id);
                            await HandleNewTranCreated(tran, id);
                        }
                    }
                }
                await TransactionAdded(tran);
            }
            if (!keepOpen)
            {
                await CloseTranForm();
            }
            IsSaving = false;
            return true;
        }

        private async Task HandleNewTranCreated(BaseTransactionForEdit tran, long newTranId)
        {
            var newTran = await transactionsService.GetTransactionAsync(newTranId);
            AddNewTransaction(newTran, tran);
            tran.Id = newTranId;
            tran.SavedTransaction = newTran.Transaction;
            if (tran is AccountTransferTransactionForEdit transfer)
            {
                transfer.SavedFrom = newTran.From;
                transfer.SavedTo = newTran.To;
            }
        }

        private async Task TranUpdated(ForEditPair pair)
        {
            if (await TransactionDeleted(pair.Original))
            {
                if (await TransactionAdded(pair.Updated))
                {
                    if (pair.ConfirmedMatchingTransferTransaction != null)
                    {
                        await TransactionDeleted(pair.ConfirmedMatchingTransferTransaction);
                    }
                }
            }
        }

        protected async Task RetryShowTransaction()
        {
            LoadSingleTransactionError = null;
            if (SelectedTransaction != null)
            {
                await ShowTransaction(SelectedTransaction, force: true);
            }
            else
            {
                await CloseTranForm();
            }
        }

        private async ValueTask<bool> TryFindDestinationTransactionForTransferTypeChange(ForEditPair pair)
        {
            if (pair.Original.TranType != TransactionType.Simple)
                return false;

            if (pair.Updated.TranType != TransactionType.Transfer)
                return false;

            var simpleOriginal = (SimpleTransactionForEdit)pair.Original;
            var transfer = (AccountTransferTransactionForEdit)pair.Updated;

            if (simpleOriginal.AccountUid == null
                || transfer.AccountUidTo == null
                || transfer.AccountUidFrom == null)
                return false;

            if (transfer.Date != simpleOriginal.Date)
            {
                return false;
            }

            if (transfer.AccountUidFrom == transfer.AccountUidTo)
            {
                return false;
            }

            //Both accounts were changed from original
            if (transfer.AccountUidTo != simpleOriginal.AccountUid
                && transfer.AccountUidFrom != simpleOriginal.AccountUid)
            {
                return false;
            }

            string accountUid;
            decimal amount;
            string currency;
            string accountName;

            if (transfer.AccountUidFrom == simpleOriginal.AccountUid)
            {
                accountUid = transfer.AccountUidTo;
                amount = -transfer.AmountTo.Value;
                currency = transfer.CurrencyTo;
                accountName = settingsService.FindAssetOrPlaidAccount(accountUid)?.Name;
            }
            else
            {
                accountUid = transfer.AccountUidFrom;
                amount = transfer.AmountFrom.Value;
                currency = transfer.CurrencyFrom;
                accountName = settingsService.FindAssetOrPlaidAccount(accountUid)?.Name;
            }

            var transactions = await transactionsService.GetTransactionsForAssetOrPlaidAsync(
                accountUid,
                startDate: transfer.Date,
                endDate: transfer.Date.AddDays(1));

            var matchingTransaction = transactions.Transactions
                    .Where(x => x.TranType == TransactionType.Simple)
                    .OrderBy(a => a.CategoryId == settingsService.Settings.TransferCategoryId
                    || (a.CategoryId != null && a.CategoryId.Value == settingsService.Settings.CrossCurrencyTransferCategoryId))
                        .FirstOrDefault(t =>
                        t.Amount == amount
                        && string.Equals(t.Currency, currency, StringComparison.InvariantCultureIgnoreCase)
                );

            if (matchingTransaction != null)
            {
                var payeeInfo = !string.IsNullOrEmpty(matchingTransaction.Payee)
                    ? $"<br/>Payee - {WebUtility.HtmlEncode(matchingTransaction.Payee)}."
                    : null;
                var notesInfo = !string.IsNullOrEmpty(matchingTransaction.Notes)
                    ? $"</br>Notes - {WebUtility.HtmlEncode(FormatService.LimitLength(matchingTransaction.Notes, 100))}."
                    : null;

                await ModalContainer.Modal.Show(new InfoModalVM
                {
                    Title = "Group suggestion",
                    Message = new MarkupString($"Found transaction with the same amount and date in {accountName ?? "account"}.{payeeInfo}{notesInfo}<br/>Do you want to make it part of the transfer?"),
                    HideCloseButton = true,
                    AdditionalButtonText = "No, ignore it",
                    AdditionalButtonColor = Color.Secondary,
                    AdditionalButtonCallback = async () =>
                    {
                        await ModalContainer.Modal.Hide();
                        pair.SkipTransferMatchingTransactionCheck = true;
                        await SaveTransaction(pair);
                    },
                    AdditionalButton2Text = "Yes, use it",
                    AdditionalButton2Color = Color.Primary,
                    AdditionalButton2Callback = async () =>
                    {
                        await ModalContainer.Modal.Hide();
                        pair.ConfirmedMatchingTransferTransaction = matchingTransaction.GetForEdit();
                        await SaveTransaction(pair);
                    }
                });

                return true;
            }

            return false;
        }

        private async Task<bool> UpdateTranAfterTypeChange(ForEditPair pair)
        {
            var original = pair.Original;
            var tran = pair.Updated;

            if (tran.TranType == TransactionType.Split)
            {
                var split = (SplitTransactionForEdit)tran;
                if (original.TranType == TransactionType.Simple)
                {
                    await transactionsService.UpdateSplitTransaction(split, TransactionType.Simple);
                }
                else if (original.TranType == TransactionType.Transfer)
                {
                    await transactionsService.DeleteTransactionGroup(original.Id);
                    var toKeepId = original.Transaction.children
                        .OrderByDescending(x => x.plaid_account_id.HasValue)
                        .ThenBy(x => x.id)
                        .Select(x => x.id)
                        .FirstOrDefault();

                    foreach (var item in original.ChildTransactionIds.Where(x => x.Id != toKeepId))
                    {
                        await transactionsService.DeleteTransaction(item.Id);
                    }
                    split.Id = toKeepId;
                    await transactionsService.UpdateSplitTransaction(split, TransactionType.Simple);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot update split transaction from {original.TranType} type");
                }

                var tranDisplay = await TryRefreshDisplayTransaction(original.Id, tran, split.Id);
                if (tranDisplay != null)
                {
                    split.SavedTransaction = tranDisplay.Transaction;
                }
                foreach (var child in split.Children)
                {
                    tranDisplay = await TryRefreshDisplayTransaction(child.Id, tran);
                    if (tranDisplay != null)
                    {
                        child.SavedTransaction = tranDisplay.Transaction;
                    }
                }
            }
            else if (tran.TranType == TransactionType.Transfer)
            {
                if (!pair.SkipTransferMatchingTransactionCheck
                    && pair.ConfirmedMatchingTransferTransaction == null
                    && await TryFindDestinationTransactionForTransferTypeChange(pair))
                {
                    return false;
                }

                var transfer = (AccountTransferTransactionForEdit)tran;

                if (original.TranType == TransactionType.Split)
                {
                    await transactionsService.UnsplitTransaction(original.Id);
                }
                else if (original.TranType == TransactionType.Simple)
                {
                    //nothing to do
                }
                else
                {
                    throw new InvalidOperationException($"Cannot update transfer transaction from {original.TranType} type");
                }

                var inserts = transfer.GetInsertDtos(settingsService);
                if (inserts.Length != 2)
                {
                    throw new InvalidOperationException("Transfer transaction should have 2 inserts");
                }

                var firstInsert = inserts.First();
                var secondInsert = inserts.Last();


                if (original.Transaction.plaid_account_id != null
                    && original.Transaction.amount < 0)
                {
                    //Swap
                    (firstInsert, secondInsert) = (secondInsert, firstInsert);
                }

                long secondId;

                if (pair.ConfirmedMatchingTransferTransaction == null)
                {
                    var newIds = await transactionsService.InsertTransactions([secondInsert]);
                    secondId = newIds.First();
                }
                else
                {
                    secondId = pair.ConfirmedMatchingTransferTransaction.Id;
                    await transactionsService.UpdateTransaction(secondInsert.GetUpdate(secondId));
                }

                var update = firstInsert.GetUpdate(original.Id);
                await transactionsService.UpdateTransaction(update);
                var group = transfer.GetGroupDto(settingsService, [original.Id, secondId]);
                var newTransferId = await transactionsService.CreateGroup(group);

                var idToRefresh = newTransferId;
                if (CurrentAccountUid != null)
                {
                    if (CurrentAccountUid == TransactionsService.GetAccountUid(update.asset_id, update.plaid_account_id))
                    {
                        idToRefresh = original.Id;
                    }
                    else if (CurrentAccountUid == TransactionsService.GetAccountUid(secondInsert.asset_id, secondInsert.plaid_account_id))
                    {
                        idToRefresh = secondId;
                    }
                }

                var tranDisplay = await TryRefreshDisplayTransaction(original.Id, tran, idToRefresh);
                if (tranDisplay != null)
                {
                    transfer.SavedTransaction = tranDisplay.Transaction;
                    transfer.SavedFrom = tranDisplay.From;
                    transfer.SavedTo = tranDisplay.To;
                    if (pair.ConfirmedMatchingTransferTransaction != null)
                    {
                        TryRemoveDisplyTransaction(pair.ConfirmedMatchingTransferTransaction.Id);
                    }
                }
                if (tranDisplay == null && pair.ConfirmedMatchingTransferTransaction != null)
                {
                    tranDisplay = await TryRefreshDisplayTransaction(
                        pair.ConfirmedMatchingTransferTransaction.Id, tran, idToRefresh);

                    if (tranDisplay != null)
                    {
                        transfer.SavedTransaction = tranDisplay.Transaction;
                        transfer.SavedFrom = tranDisplay.From;
                        transfer.SavedTo = tranDisplay.To;
                    }
                }
                if (tranDisplay == null && original is SplitTransactionForEdit split)
                {
                    //If split transaction was not found, maybe we started edit via one of it's children
                    foreach (var child in split.Children)
                    {
                        tranDisplay = await TryRefreshDisplayTransaction(child.Id, tran, idToRefresh);
                        if (tranDisplay != null)
                        {
                            transfer.SavedTransaction = tranDisplay.Transaction;
                            transfer.SavedFrom = tranDisplay.From;
                            transfer.SavedTo = tranDisplay.To;
                            break;
                        }
                    }
                }

            }
            else if (tran.TranType == TransactionType.Simple)
            {
                if (original.TranType == TransactionType.Split)
                {
                    await transactionsService.UnsplitTransaction(original.Id);
                }
                else if (original.TranType == TransactionType.Transfer)
                {
                    await transactionsService.DeleteTransactionGroup(original.Id);

                    var toKeepId = original.Transaction.children
                        .OrderByDescending(x => x.plaid_account_id.HasValue)
                        .ThenBy(x => x.id)
                        .Select(x => x.id)
                        .FirstOrDefault();

                    foreach (var item in original.ChildTransactionIds.Where(x => x.Id != toKeepId))
                    {
                        await transactionsService.DeleteTransaction(item.Id);
                    }

                    tran.Id = toKeepId;
                }

                var update = tran.GetUpdateDtos(settingsService).Single();
                update.id = tran.Id;
                await transactionsService.UpdateTransaction(update);
                var tranDisplay = await TryRefreshDisplayTransaction(original.Id, tran, tran.Id);
                if (tranDisplay != null)
                {
                    tran.SavedTransaction = tranDisplay.Transaction;
                }
                else
                {
                    //If transaction was not found, maybe we started edit via one of it's children
                    foreach (var child in original.ChildTransactionIds)
                    {
                        tranDisplay = await TryRefreshDisplayTransaction(child.Id, tran, tran.Id);
                        if (tranDisplay != null)
                        {
                            tran.SavedTransaction = tranDisplay.Transaction;
                            break;
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"Cannot update transaction to {tran.TranType} type");
            }

            return true;
        }

        protected virtual string CurrentAccountUid => null;

        private async Task UpdateTranSameType(BaseTransactionForEdit tran)
        {
            if (tran.TranType == TransactionType.Split)
            {
                var split = (SplitTransactionForEdit)tran;
                await transactionsService.UpdateSplitTransaction(split, TransactionType.Split);
                var tranDisplay = await TryRefreshDisplayTransaction(split.Id, tran);
                if (tranDisplay != null)
                {
                    split.SavedTransaction = tranDisplay.Transaction;
                }
                foreach (var child in split.Children)
                {
                    tranDisplay = await TryRefreshDisplayTransaction(child.Id, tran);
                    if (tranDisplay != null)
                    {
                        child.SavedTransaction = tranDisplay.Transaction;
                    }
                }
            }
            else
            {
                var updates = tran.GetUpdateDtos(settingsService);
                foreach (var update in updates)
                {
                    await transactionsService.UpdateTransaction(update);
                    var tranDisplay = await TryRefreshDisplayTransaction(update.id, tran);
                    if (tranDisplay != null)
                    {
                        tran.SavedTransaction = tranDisplay.Transaction;
                    }
                }
            }
        }

        public async Task OnTranListScrollEnd()
        {
            if (!HasMoreTrans || LoadingMoreTrans)
            {
                return;
            }
            await LoadMoreTransactions();
        }
    }
}
