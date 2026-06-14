using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HuflitShopCore.Models
{
    [Table("Suppliers")]
    public class Supplier
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(255)]
        public string SupplierName { get; set; }

        [StringLength(255)]
        public string ContactName { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(255)]
        public string Email { get; set; }

        public string Address { get; set; }

        public virtual ICollection<StockReceived> StockReceiveds { get; set; }
    }
}