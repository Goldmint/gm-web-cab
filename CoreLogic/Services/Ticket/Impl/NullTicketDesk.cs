using Goldmint.Common;
using Goldmint.DAL.Models;
using NLog;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Ticket.Impl {

	public class NullTicketDesk : ITicketDesk {

		private ILogger _logger;

		public NullTicketDesk(LogFactory logFactory) {
			_logger = logFactory.GetLoggerFor(this);
		}

		// ---

		public Task<string> CreateCardVerificationTicket(string parentTicketId, TicketStatus status, string username, FiatCurrency currency, string message) {
			_logger?.Info("New Verification Ticket: " + message);
			return Task.FromResult(SecureRandom.GetString09af(20));
		}

		public Task UpdateCardVerificationTicket(string ticketId, TicketStatus status, string message) {
			_logger?.Info("Update Verification Ticket: " + message);
			return Task.CompletedTask;
		}

		public Task<string> CreateCardDepositTicket(TicketStatus status, string username, long amount, FiatCurrency currency, string message) {
			_logger?.Info("New Deposit Ticket: " + message);
			return Task.FromResult(SecureRandom.GetString09af(20));
		}

		public Task UpdateCardDepositTicket(string ticketId, TicketStatus status, string message) {
			_logger?.Info("Update Deposit Ticket: " + message);
			return Task.CompletedTask;
		}

		public Task<string> CreateCardRefundTicket(string parentTicketId, TicketStatus status, string username, long amount, FiatCurrency currency, string message) {
			_logger?.Info("New Refund Ticket: " + message);
			return Task.FromResult(SecureRandom.GetString09af(20));
		}

		public Task UpdateCardRefundTicket(string ticketId, TicketStatus status, string message) {
			_logger?.Info("Update Refund Ticket: " + message);
			return Task.CompletedTask;
		}

		public Task<string> CreateCardWithdrawTicket(TicketStatus status, string username, long amount, FiatCurrency currency, string message) {
			_logger?.Info("New Withdraw Ticket: " + message);
			return Task.FromResult(SecureRandom.GetString09af(20));
		}

		public Task UpdateCardWithdrawTicket(string ticketId, TicketStatus status, string message) {
			_logger?.Info("Update Withdraw Ticket: " + message);
			return Task.CompletedTask;
		}

		public Task<string> CreateSupportWithdrawTicket(string parentTicketId, Withdraw withdrawModel, string message) {
			_logger?.Info("New Support Withdraw Ticket: " + message);
			return Task.FromResult(SecureRandom.GetString09af(20));
		}

		public Task<string> CreateGoldBuyingTicket(TicketStatus status, string username, string message) {
			_logger?.Info("New Gold Buying Ticket: " + message);
			return Task.FromResult(SecureRandom.GetString09af(20));
		}

		public Task UpdateGoldBuyingTicket(string ticketId, TicketStatus status, string message) {
			_logger?.Info("Update Gold Buying Ticket: " + message);
			return Task.CompletedTask;
		}

		public Task<string> CreateGoldSellingTicket(TicketStatus status, string username, string message) {
			_logger?.Info("New Gold Selling Ticket: " + message);
			return Task.FromResult(SecureRandom.GetString09af(20));
		}

		public Task UpdateGoldSellingTicket(string ticketId, TicketStatus status, string message) {
			_logger?.Info("Update Gold Selling Ticket: " + message);
			return Task.CompletedTask;
		}
	}
}
