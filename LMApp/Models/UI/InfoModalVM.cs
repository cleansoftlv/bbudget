using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace LMApp.Models.UI
{
    public class InfoModalVM
    {
        public string Title { get; set; }

        public MarkupString Message { get; set; }


        public Size ModalSize { get; set; } = Size.Small;

        public string AdditionalButtonText { get; set; }
        public Color AdditionalButtonColor { get; set; } = Color.Primary;

        public bool AdditionalButtonIsOutline { get; set; } = false;

        public Func<Task> AdditionalButtonCallback { get; set; }

        public string AdditionalButton2Text { get; set; }
        public Color AdditionalButton2Color { get; set; } = Color.Primary;
        public bool AdditionalButton2IsOutline { get; set; } = false;

        public Func<Task> AdditionalButton2Callback { get; set; }

        public bool HideCloseButton { get; set; }

        public Func<Task> CloseCallback { get; set; }

        public int MyProperty { get; set; }


    }
}
