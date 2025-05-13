using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    //bảng trung gian của style với dịch vụ
    public class DecorServiceStyle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int DecorServiceId { get; set; }
        public DecorService DecorService { get; set; }

        public int DecorationStyleId { get; set; }
        public DecorationStyle DecorationStyle { get; set; }
    }
}
