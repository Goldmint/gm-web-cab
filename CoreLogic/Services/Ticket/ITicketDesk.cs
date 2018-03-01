using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.DAL.Models.Identity;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Ticket {

	public interface ITicketDesk {

		Task UpdateTicket(string ticketId, UserOpLogStatus status, string message);
		Task<string> NewCardVerification(User user, Card card);
		Task<string> NewCardDeposit(User user, Card card, FiatCurrency currency, long amount);
		Task<string> NewSwiftDeposit(User user, FiatCurrency currency, long amount);
		Task<string> NewCardWithdraw(User user, Card card, FiatCurrency currency, long amount);
		Task<string> NewSwiftWithdraw(User user, FiatCurrency currency, long amount);
		Task<string> NewGoldBuying(User user, string ethAddressOrNull, FiatCurrency currency, long fiatAmount, long rate, BigInteger mntpBalance, BigInteger estimatedGoldAmount, long feeCents);
		Task<string> NewGoldSelling(User user, string ethAddressOrNull, FiatCurrency currency, BigInteger goldAmount, long rate, BigInteger mntpBalance, long estimatedFiatAmount, long feeCents);
		Task<string> NewGoldTransfer(User user, string ethAddress, BigInteger goldAmount);

	}
}
