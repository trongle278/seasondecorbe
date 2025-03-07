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
        [NotMapped]
        public double Rate { get; set; }
        [NotMapped]
        public int TotalRate { get; set; }
        [NotMapped]
        public int TotalSold { get; set; }
        public string? Description { get; set; }
        public double ProductPrice { get; set; }
        public int? Quantity { get; set; }
        public string? MadeIn { get; set; }
        public string? ShipFrom { get; set; }
        public DateTime CreateAt { get; set; }
        public enum ProductStatus
        {
            InStock,
            OutOfStock,
            InComing
        }
        public ProductStatus Status { get; set; }

        public int CategoryId { get; set; }
        public ProductCategory Category { get; set; }

        public int ProviderId { get; set; }
        public Provider Provider { get; set; }

        public virtual ICollection<CartItem> CartItems { get; set; }
        public virtual ICollection<ProductOrder> ProductOrders { get; set; }
        public virtual ICollection<ProductImage>? ProductImages { get; set; }
    }
}
