using BootstrapBlazor.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Controls.Bootstrap
{
    [JSModuleAutoLoader("./_content/BootstrapBlazor/Components/Select/Select.razor.js",
        JSObjectReference = true)]
    public class SearchEntitySelectString : SearchSelect<string>
    {
        public SearchEntitySelectString():base()
        {
            AddEmptyItem = true;
        }
    }
}
