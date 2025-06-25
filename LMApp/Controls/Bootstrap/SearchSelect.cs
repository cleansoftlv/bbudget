using BlazorApplicationInsights.Models;
using BootstrapBlazor.Components;
using LMApp.Models.UI;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Controls.Bootstrap
{
    [JSModuleAutoLoader("./_content/BootstrapBlazor/Components/Select/Select.razor.js", JSObjectReference = true)]
    public class SearchSelect<T> : Select<T>
    {

        private static readonly SelectedItem EmptyItem
              = new SelectedItem { Text = "", Value = "" };

        public SearchSelect() : base()
        {
            ScrollIntoViewBehavior = ScrollIntoViewBehavior.Instant;
            ItemTemplate = FormatService.SelectItemTemplateWithEmpty;
            ShowLabel = true;
            ShowSearch = true;
        }

        [Parameter]
        public bool AddEmptyItem { get; set; }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);
            if (parameters.TryGetValue<IEnumerable<SelectedItem>>(nameof(Items), out var value))
            {
                Items = Items.Prepend(EmptyItem);
            }
        }

        [Inject]
        public Utils Utils { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                if (string.IsNullOrEmpty(Id))
                {
                    throw new InvalidOperationException("Id is required for SearchSelect");
                }
                await Utils.FixBootstrapEnterHandlingById(Id);
            }
        }

        public async Task FocusAsync()
        {
           await Utils.FocusById(Id);
        }
    }
}
