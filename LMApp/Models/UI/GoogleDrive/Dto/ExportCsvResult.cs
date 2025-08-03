using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.UI.GoogleDrive.Dto
{
    public class ExportCsvResult
    {

        public bool Success { get; set; }
        public string FileId { get; set; }
        public string WebViewLink { get; set; }
        public string Message { get; set; }
    }
}
