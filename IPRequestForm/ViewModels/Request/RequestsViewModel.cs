using System;
using System.Collections.Generic;
using System.Linq;
using IPRequestForm.Controllers;
using IPRequestForm.Models;

namespace IPRequestForm.ViewModels
{
    public class RequestsViewModel : ViewModelBase
    {
        public int CurrentPage { get; set; }

        public int? NextPage { get; set; }

        public int? PreviousPage { get; set; }

        public int PageSize { get; set; }

        public int Count { get; set; }

        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public string Query { get; set; }

        public IEnumerable<RequestViewModel> Requests { get; set; }

        public RequestsViewModel(IQueryable<Request> requests, int pageNo, int pageSize, User user, RequestFilters requestFilter)
        {
            User = user;
            RequestFilter = requestFilter;

            CurrentPage = pageNo;
            PageSize = pageSize;
            TotalResults = requests.Count();

            TotalPages = (int)Math.Ceiling(TotalResults / (float)PageSize);

            if (TotalPages > CurrentPage)
            {
                NextPage = CurrentPage + 1;
            }

            if (CurrentPage > 1)
            {
                PreviousPage = CurrentPage - 1;
            }

            Count = (TotalResults > pageNo * pageSize) ? pageSize : TotalResults - (pageNo - 1) * pageSize;

            if (Count > 0)
            {
                Requests = requests.Skip((pageNo - 1) * pageSize).Take(Count).AsEnumerable().Select(i => new RequestViewModel(user, i));
            }
            else
            {
                Count = 0;

                Requests = new List<RequestViewModel>();
            }
        }
    }
}