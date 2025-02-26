using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public string ProductImg { get; set; }
        public double ProductPrice { get; set; }
        public int? Quantity { get; set; }

        public int CategoryId { get; set; }
        public ProductCategory Category { get; set; }

        public virtual ICollection<CartItem> CartItems { get; set; }
        public virtual ICollection<ProductOrder> ProductOrders { get; set; }
        public virtual ICollection<ProductImage> ProductImages { get; set; }
    }
}
