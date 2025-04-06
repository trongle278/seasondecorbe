using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelResponse.Product;

namespace BusinessLogicLayer.ModelResponse.Favorite
{
    public class FavoriteProductResponse
    {
        public int id { get; set; }
        public ProductDetailResponse ProductDetail { get; set; }
    }
}
