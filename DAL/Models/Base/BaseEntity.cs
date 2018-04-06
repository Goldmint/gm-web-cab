using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Goldmint.DAL.Models {

    public abstract class BaseEntity {

        [Key, Column("id")]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }

    }
}
