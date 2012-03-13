using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IPRequestForm.Controllers;
using IPRequestForm.Models;

namespace IPRequestForm.ViewModels
{
    public class ViewModelBase
    {
        public RequestFilters RequestFilter { get; set; }

        public User User { get; set; }
    }
}