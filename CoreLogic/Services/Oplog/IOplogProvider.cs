using Goldmint.Common;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Oplog {

	public interface IOplogProvider {

		Task Update(string oplogId, UserOpLogStatus status, string message);

		Task<string> NewGoldBuyingRequestForCryptoasset(long userId, EthereumToken ethereumToken, string destAddress, FiatCurrency fiatCurrency, long inputRate, long goldRate, string promoCode);
		Task<string> NewGoldSellingRequestForCryptoasset(long userId, EthereumToken ethereumToken, string destAddress, FiatCurrency fiatCurrency, long outputRate, long goldRate);
		Task<string> NewGoldTransfer(long userId, string ethAddress, BigInteger goldAmount);
		Task<string> NewCardVerification(long userId, long cardId, long centsAmount, FiatCurrency fiatCurrency);
		Task<string> NewGoldBuyingRequestWithCreditCard(long userId, string destAddress, FiatCurrency fiatCurrency, long goldRate, long centsAmount, string promoCode);
		Task<string> NewGoldSellingRequestWithCreditCard(long userId, string destAddress, FiatCurrency fiatCurrency, long goldRate);
	}
}
