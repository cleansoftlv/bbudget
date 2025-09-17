using BootstrapBlazor.Components;
using LMApp.Models.UI;

namespace LMApp.Pages
{
    public abstract class BaseTransactionFilterPage: BaseTransactionPage
    {
       

        protected TransactionListContext transactionListContext = TransactionListContext.None;

        protected bool ShowTranList;
        protected bool LoadingTransactions = false;
        protected bool TranListClosing;


        protected string PrimaryPanelClass()
        {
            if (!ShowTranForm && !ShowTranList)
            {
                return "col-12" +
                    " col-lg-6 offset-lg-3" +
                    " col-xxl-4 offset-xxl-4";
            }
            else if (ShowTranForm != ShowTranList)
            {
                return "col-12 d-none" +
                    " col-lg-6 d-lg-flex" +
                    " col-xxl-4 offset-xxl-2";
            }
            else
            {
                return "col-12 d-none" +
                    " col-xxl-4 d-xxl-flex";
            }
        }

        protected string TransactionListClass()
        {
            if (ShowTranList)
            {
                if (ShowTranForm)
                {
                    return "col-12 d-none" +
                        " col-lg-6 d-lg-block" +
                        " col-xxl-4";
                }
                else
                {
                    return "col-12" +
                        " col-lg-6" +
                        " col-xxl-4";
                }
            }
            else
            {
                return "d-none";
            }
        }

        protected string TransactionFormClass()
        {
            if (ShowTranForm)
            {
                if (ShowTranList)
                {
                    return "col-12" +
                        " col-lg-6" +
                        " col-xxl-4";
                }
                else
                {
                    return "col-12" +
                        " col-lg-6" +
                        " col-xxl-4";
                }
            }
            else
            {
                return "d-none";
            }
        }

        protected virtual async Task<bool> CloseTranList()
        {
            if (!ShowTranList)
                return false;

            TranListClosing = true;
            await Task.Yield();

            await StartTransition(BreakPoint.Large);
            DoCloseTranList();
            await EndTransition();

            TranListClosing = false;

            return true;
        }

        protected void DoCloseTranList()
        {
            if (!ShowTranList)
                return;

            HasMoreTrans = false;
            LoadingMoreTrans = false;
            Transactions = null;
            transactionListContext = TransactionListContext.None;
            ShowTranList = false;
            RefreshActivePage();
        }
    }
}
