﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class DecorationStyle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; } // Modern, Scandinavian, etc.
        //public ICollection<Account> Accounts { get; set; }

        public virtual ICollection<DecorServiceStyle> DecorServiceStyles { get; set; } = new List<DecorServiceStyle>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
