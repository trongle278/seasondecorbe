using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse
{
    public class DecorCategoryDTO
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }
    public class DecorCategoryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
        public DecorCategoryDTO Data { get; set; }  // Single item

        public DecorCategoryResponse()
        {
            Errors = new List<string>();
        }
    }

    public class DecorCategoryListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
        public List<DecorCategoryDTO> Data { get; set; }  // List of items

        public DecorCategoryListResponse()
        {
            Errors = new List<string>();
            Data = new List<DecorCategoryDTO>();
        }
    }
}
