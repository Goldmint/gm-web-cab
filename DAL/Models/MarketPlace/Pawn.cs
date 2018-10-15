using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goldmint.DAL.Models.MarketPlace
{
	[Table("gm_pawn")]
	public class Pawn : BaseEntity, IConcurrentUpdate
	{
		[Column("status"), Required]
		public int Status { get; set; }

	    [Column("number"), MaxLength(64), Required]
	    public string Number { get; set; }

	    [Column("annual_interest"), Required]
	    public double AnnualInterest { get; set; }

	    [Column("description"), MaxLength(2048), Required]
	    public string Description { get; set; }

	    [Column("weight"), Required]
	    public double Weight { get; set; }

	    [Column("purity"), Required]
	    public double Purity { get; set; }

	    [Column("token"), Required]
	    public decimal Token { get; set; }

	    [Column("started_at"), Required]
	    public DateTime Started { get; set; }

	    [Column("expires_at"), Required]
	    public DateTime Expires { get; set; }

	    [Column("closed_at"), Required]
	    public DateTime Closed { get; set; }

	    [Column("client_id"), Required]
	    public long? ClientId { get; set; }

        [Column("concurrency_stamp"), MaxLength(FieldMaxLength.ConcurrencyStamp), ConcurrencyCheck]
		public string ConcurrencyStamp { get; set; }

		// ---

		public void OnConcurrencyStampRegen()
		{
			this.ConcurrencyStamp = ConcurrentStamp.GetGuid();
		}
	}
}
