﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class ProviderResponse
    {
        public int Id { get; set; }
        public string BusinessName { get; set; }
        public string Slug { get; set; }
        public string Bio { get; set; }
        public string Avatar { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public bool IsProvider { get; set; }
        public bool ProviderVerified { get; set; }
        public int ProviderStatus { get; set; }

        public string JoinedDate { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingsCount { get; set; }
    }

    public class ProductProviderResponse
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string BusinessName { get; set; }
        public string avatar { get; set; }
        public int TotalRate { get; set; }
        public int TotalProduct { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingsCount { get; set; }
    }
}
