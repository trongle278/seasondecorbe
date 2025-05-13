using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    //bảng trung gian màu với dịch vụ
    public class DecorServiceThemeColor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int DecorServiceId { get; set; }
        public DecorService DecorService { get; set; }

        public int ThemeColorId { get; set; }
        public ThemeColor ThemeColor { get; set; }
    }
}
