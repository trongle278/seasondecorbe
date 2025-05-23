﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class ProviderApplicationFilterRequest
    {
        public string? Fullname { get; set; }
        public bool? ProviderVerified { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreateAt";
        public bool Descending { get; set; } = false;
    }
}
