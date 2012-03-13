using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IPRequestForm.Models;

namespace IPRequestForm.ViewModels
{
    public class SignUpViewModel : ViewModelBase
    {
        public IEnumerable<Department> Departments { get; set; }

        public IEnumerable<string> Roles { get; set; }
    }
}