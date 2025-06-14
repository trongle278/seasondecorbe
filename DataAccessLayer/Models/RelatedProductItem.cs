﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class RelatedProductItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RelatedProductId { get; set; }
        public RelatedProduct RelatedProduct { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string ProductName { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
