using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.Common;
using Goldmint.DAL.Models;

namespace Goldmint.DAL.CustodyBotModels
{
	[Table("files")]
	public class Files : BaseEntity
	{
		[Column("created_at")]
		public DateTime CreatedAt { get; set; }

	    [Column("updated_at")]
	    public DateTime UpdatedAt { get; set; }

	    [Column("name"), MaxLength(FieldMaxLength.CustodyFileName)]
	    public string Name { get; set; }

	    [Column("extension"), MaxLength(FieldMaxLength.CustodyFileExtension)]
	    public string Extension { get; set; }

        [Column("type")]
	    public UploadType Type { get; set; }

	    [Column("uploaded")]
	    public bool Uploaded { get; set; }

	    [Column("client_id")]
	    public long ClientId { get; set; }
    }
}
