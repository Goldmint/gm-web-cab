﻿using Goldmint.Common;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Oplog {

	public interface IOplogProvider {

		Task Update(string oplogId, UserOpLogStatus status, string message);

		Task<string> NewGoldBuyingRequestForCryptoasset(long userId, CryptoCurrency cryptoCurrency, string destAddress, FiatCurrency fiatCurrency, long inputRate, long goldRate);
		Task<string> NewGoldSellingRequestForCryptoasset(long userId, CryptoCurrency cryptoCurrency, string destAddress, FiatCurrency fiatCurrency, long outputRate, long goldRate);
		Task<string> NewGoldTransfer(long userId, string ethAddress, BigInteger goldAmount);
		Task<string> NewCardVerification(long userId, long cardId, long centsAmount, FiatCurrency fiatCurrency);
		Task<string> NewGoldBuyingRequestWithCreditCard(long userId, string destAddress, FiatCurrency fiatCurrency, long goldRate, long centsAmount);
		Task<string> NewGoldSellingRequestWithCreditCard(long userId, string destAddress, FiatCurrency fiatCurrency, long goldRate);
	}
}