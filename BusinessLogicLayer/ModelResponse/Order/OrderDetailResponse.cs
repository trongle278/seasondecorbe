﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse.Order
{
    public class OrderDetailResponse
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public bool? IsReviewed { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public OrderProviderResponse? Provider { get; set; }
    }
}
