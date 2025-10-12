using BootstrapBlazor.Components;
using LMApp.Models.Transactions;
using LMApp.Models.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace LMApp.Controls
{
    public abstract class BaseNestedTransactionForm : ComponentBase, IAsyncDisposable
    {
        [Inject]
        public Utils utils { get; set; }

        [Parameter]
        public EventCallback Cancel { get; set; }

        [Parameter]
        public EventCallback Delete { get; set; }

        [Parameter]
        public EventCallback NavigateLeft { get; set; }

        [Parameter]
        public EventCallback<BaseTransactionForEdit> CopyTransaction { get; set; }

        [Parameter]
        public EventCallback<BaseTransactionForEdit> ShareTransaction { get; set; }

        [Parameter]
        public EventCallback SaveAndNextCallback { get; set; }

        [Parameter]
        public EventCallback NextTransactionCallback { get; set; }

        [Parameter]
        public EventCallback CreateTransferCallback { get; set; }

        protected async Task OnCopy()
        {
            await CopyTransaction.InvokeAsync(BaseTran);
        }

        protected async Task OnShare()
        {
            await ShareTransaction.InvokeAsync(BaseTran);
        }

        [Parameter]
        public bool ShowAndNextOnSave { get; set; }
        protected abstract BaseTransactionForEdit BaseTran { get; }

        protected bool SaveDisabled => !ForceHasChanges && (IsSaving || !BaseTran.HasChanges);

        protected Color SaveButtonColor => SaveDisabled ? Color.Secondary : Color.Primary;

        protected string SaveButtonText => BaseTran.Id <= 0 ? "Create" : "Save";


        protected ElementReference RootDiv;
        private string rootDivId = null;
        private DotNetObjectReference<BaseNestedTransactionForm> dotNetHelper;
        private bool hotkeysRegistered = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetHelper = DotNetObjectReference.Create(this);
                await utils.RegisterTransactionFormHotkeys(dotNetHelper);
                hotkeysRegistered = true;
            }

            if (rootDivId != RootDiv.Id)
            {
                rootDivId = RootDiv.Id;
                await utils.FixEnterIssue(RootDiv);
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        [Parameter]
        public bool IsSaving { get; set; }


        [Parameter]
        public bool ForceHasChanges { get; set; }

        protected Task OnCancel()
        {
            return Cancel.InvokeAsync();
        }

        protected Task OnDelete()
        {
            return Delete.InvokeAsync();
        }

        protected Task OnNavigateLeft()
        {
            return NavigateLeft.InvokeAsync();
        }


        [JSInvokable]
        public async Task HandleHotkey(string action)
        {
            switch (action.ToLower())
            {
                case "save":
                    if (!SaveDisabled)
                    {
                        await OnSave();
                    }
                    break;
                case "saveandnext":
                    if (SaveAndNextCallback.HasDelegate)
                    {
                        await SaveAndNextCallback.InvokeAsync();
                    }
                    break;
                case "nextwithoutsaving":
                    if (NextTransactionCallback.HasDelegate)
                    {
                        await NextTransactionCallback.InvokeAsync();
                    }
                    break;
                case "delete":
                    if (BaseTran.Id > 0)
                    {
                        await OnDelete();
                    }
                    break;
                case "navleft":
                    await OnNavigateLeft();
                    break;
                case "copy":
                    if (BaseTran.Id > 0)
                    {
                        await OnCopy();
                    }
                    break;
                case "createtransfer":
                    if (CreateTransferCallback.HasDelegate && BaseTran.Id > 0)
                    {
                        await CreateTransferCallback.InvokeAsync();
                    }
                    break;
            }
        }

        protected virtual async Task OnSave()
        {
            // Override in derived classes to implement save logic
            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (hotkeysRegistered)
            {
                try
                {
                    await utils.UnregisterTransactionFormHotkeys();
                    hotkeysRegistered = false;
                }
                catch (JSDisconnectedException)
                {
                    // Ignore if JavaScript is disconnected
                }
                catch (ObjectDisposedException)
                {
                    // Ignore if utils is already disposed
                }
            }

            dotNetHelper?.Dispose();
        }
    }
}
