using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Utilities.DataMapping
{
    public static class DecorCategoryMapping
    {
        public static readonly Dictionary<string, List<string>> DecorToProductCategoryMap = new()
        {
            ["Living Room"] = new() { "Sofa", "Lamp", "Clock", "Table", "Couch" },
            ["Bedroom"] = new() { "Bed", "Closet", "Vanity", "Hanger", "Chest", "Lamp" },
            ["Kitchen"] = new() { "Cabinet" },
            ["Bathroom"] = new() { "Cabinet" },
            ["Home Office"] = new() { "Desk", "Chair", "Bookshelf" },
            ["Balcony & Garden"] = new() { "Lamp", "Chair" },
            ["Dining Room"] = new() { "Table", "Chair" },
            ["Entertainment Room"] = new() { "Couch", "Table", "Lamp" }
        };
    }
}
