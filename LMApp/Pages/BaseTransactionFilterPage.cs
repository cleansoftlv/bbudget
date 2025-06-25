using LMApp.Models.UI;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbelt.Blazor.ViewTransition;

namespace LMApp.Pages
{
    public abstract class BaseTransactionFilterPage: BaseTransactionPage
    {
       

        protected TransactionListContext transactionListContext = TransactionListContext.None;

        protected bool ShowTranList;
        protected bool LoadingTransactions = false;

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


    }
}
