using Goldmint.Common;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Ticket {

	public interface ITicketDesk {

		Task<string> CreateCardVerificationTicket(string parentTicketId, TicketStatus status, string username, FiatCurrency currency, string message);
		Task UpdateCardVerificationTicket(string ticketId, TicketStatus status, string message);

		Task<string> CreateCardDepositTicket(TicketStatus status, string username, long amount, FiatCurrency currency, string message);
		Task UpdateCardDepositTicket(string ticketId, TicketStatus status, string message);

		Task<string> CreateSwiftDepositTicket(TicketStatus status, string username, long amount, FiatCurrency currency, string message);
		Task UpdateSwiftDepositTicket(string ticketId, TicketStatus status, string message);

		Task<string> CreateCardRefundTicket(string parentTicketId, TicketStatus status, string username, long amount, FiatCurrency currency, string message);
		Task UpdateCardRefundTicket(string ticketId, TicketStatus status, string message);

		Task<string> CreateCardWithdrawTicket(TicketStatus status, string username, long amount, FiatCurrency currency, string message);
		Task UpdateCardWithdrawTicket(string ticketId, TicketStatus status, string message);

		Task<string> CreateSwiftWithdrawTicket(TicketStatus status, string username, long amount, FiatCurrency currency, string message);
		Task UpdateSwiftWithdrawTicket(string ticketId, TicketStatus status, string message);

		Task<string> CreateSupportWithdrawTicket(string parentTicketId, DAL.Models.Withdraw withdrawModel, string message);

		Task<string> CreateGoldBuyingTicket(TicketStatus status, string username, string message);
		Task UpdateGoldBuyingTicket(string ticketId, TicketStatus status, string message);

		Task<string> CreateGoldSellingTicket(TicketStatus status, string username, string message);
		Task UpdateGoldSellingTicket(string ticketId, TicketStatus status, string message);
	}
}
