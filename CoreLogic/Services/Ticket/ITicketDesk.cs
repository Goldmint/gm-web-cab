using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.DAL.Models.Identity;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Ticket {

	public interface ITicketDesk {

		Task UpdateTicket(string ticketId, UserOpLogStatus status, string message);
		Task<string> NewCardVerification(DAL.Models.Identity.User user, Card card);
		Task<string> NewCardDeposit(DAL.Models.Identity.User user, Card card, FiatCurrency currency, long amount);
		Task<string> NewCardWithdraw(DAL.Models.Identity.User user, Card card, FiatCurrency currency, long amount);
		Task<string> NewSwiftDeposit(DAL.Models.Identity.User user, FiatCurrency currency, long amount);
		Task<string> NewSwiftWithdraw(DAL.Models.Identity.User user, FiatCurrency currency, long amount);
		Task<string> NewCryptoDeposit(DAL.Models.Identity.User user, CryptoExchangeRequestOrigin origin, string address, string amount);
		Task<string> NewCryptoWithdraw(DAL.Models.Identity.User user, CryptoExchangeRequestOrigin origin, string address, FiatCurrency currency, long amount);
		Task<string> NewGoldBuying(DAL.Models.Identity.User user, string ethAddressOrNull, FiatCurrency currency, long fiatAmount, long rate, BigInteger mntpBalance, BigInteger estimatedGoldAmount, long feeCents);
		Task<string> NewGoldSelling(DAL.Models.Identity.User user, string ethAddressOrNull, FiatCurrency currency, BigInteger goldAmount, long rate, BigInteger mntpBalance, long estimatedFiatAmount, long feeCents);
		Task<string> NewGoldTransfer(DAL.Models.Identity.User user, string ethAddress, BigInteger goldAmount);
		

	}
}
