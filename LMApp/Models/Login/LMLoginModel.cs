using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMApp.Models.Login
{
    public class LMLoginModel
    {
        [Required]
        [Display(Name = "Lunch Money Dev Token")]
        public string DevToken { get; set; }
    }
}
