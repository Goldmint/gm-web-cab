using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.DAL.Models;

namespace Goldmint.DAL.CustodyBotModels
{
	[Table("custodies")]
	public class Custodies : BaseEntity
	{
		[Column("created_at")]
		public DateTime CreatedAt { get; set; }

	    [Column("updated_at")]
	    public DateTime UpdatedAt { get; set; }

	    [Column("status")]
	    public int Status { get; set; }

	    [Column("cell_number")]
	    public ulong CellNumber { get; set; }

	    [Column("locker")]
	    public ulong Locker { get; set; }

	    [Column("client_id")]
	    public ulong ClientId { get; set; }

	    [Column("measure_id")]
	    public ulong MeasureId { get; set; }

	    [Column("file_id")]
	    public ulong? FileId { get; set; }

        [Column("ticket"), MaxLength(FieldMaxLength.CustodyTicket)]
	    public string Ticket { get; set; }

	    
    }
}
