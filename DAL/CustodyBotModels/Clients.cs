using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Goldmint.Common;
using Goldmint.DAL.Models;

namespace Goldmint.DAL.CustodyBotModels
{
	[Table("clients")]
	public class Clients : BaseEntity
	{
		[Column("created_at")]
		public DateTime CreatedAt { get; set; }

	    [Column("updated_at")]
	    public DateTime UpdatedAt { get; set; }

	    [Column("name"), MaxLength(FieldMaxLength.CustodyBotName)]
	    public string Name { get; set; }

	    [Column("role")]
	    public ClientRole Role { get; set; }

	    [Column("auth_salt"), MaxLength(FieldMaxLength.CustodyBotSalt)]
	    public string AuthSalt { get; set; }

	    [Column("access_salt"), MaxLength(FieldMaxLength.CustodyBotSalt)]
	    public string AccessSalt { get; set; }

	    [Column("bot_hardware_id"), MaxLength(FieldMaxLength.CustodyBotId)]
	    public string BotHardwareId { get; set; }

	    [Column("sumus_address"), MaxLength(FieldMaxLength.BlockchainAddress)]
	    public string SumusAddress { get; set; }

	    [Column("org_id")]
	    public long OrgId { get; set; }

	    [Column("fiat_payment_route"), MaxLength(FieldMaxLength.CustodyPaymentRoute)]
	    public string FiatPaymentRoute { get; set; }
    }
}
