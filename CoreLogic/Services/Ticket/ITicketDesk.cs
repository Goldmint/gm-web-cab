using Goldmint.Common;
using Goldmint.DAL.Models;
using Goldmint.DAL.Models.Identity;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Ticket {

	public interface ITicketDesk {

		Task UpdateTicket(string ticketId, UserOpLogStatus status, string message);

		Task<string> NewGoldBuyingRequestForCryptoasset(long userId, CryptoCurrency cryptoCurrency, string destAddress, FiatCurrency fiatCurrency, long inputRate, long goldRate);
		Task<string> NewGoldSellingRequestForCryptoasset(long userId, CryptoCurrency cryptoCurrency, string destAddress, FiatCurrency fiatCurrency, long outputRate, long goldRate);
		Task<string> NewGoldTransfer(long userId, string ethAddress, BigInteger goldAmount);
		

	}
}
