using BootstrapBlazor.Components;
using LMApp.Models.UI;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Controls.Bootstrap
{
    [JSModuleAutoLoader("./_content/BootstrapBlazor/Components/DateTimePicker/DateTimePicker.razor.js", JSObjectReference = true)]

    public class DateTimePickerExt:DateTimePicker<DateTime>
    {
        public DateTimePickerExt()
        {
            IsEditable = true;
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

    }
}
