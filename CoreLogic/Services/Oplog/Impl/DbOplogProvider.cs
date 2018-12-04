using Goldmint.Common;
using NLog;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Goldmint.Common.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.CoreLogic.Services.Oplog.Impl {

	public class DbOplogProvider : IOplogProvider {

		private readonly DAL.ApplicationDbContext _dbContext;
		private ILogger _logger; 

		public DbOplogProvider(DAL.ApplicationDbContext dbContext, LogFactory logFactory) {
			_dbContext = dbContext;
			_logger = logFactory.GetLoggerFor(this);
		}

		// ---

		private async Task<string> CreateEntry(long userId, string message, long? refId = null, UserOpLogStatus status = UserOpLogStatus.Pending) {
			var op = new DAL.Models.UserOpLog() {
				Status = status,
				UserId = userId,
				RefId = refId,
				Message = message.Limit(DAL.Models.FieldMaxLength.Comment),
				TimeCreated = DateTime.UtcNow,
			};
			_dbContext.UserOpLog.Add(op);
			await _dbContext.SaveChangesAsync();
			_dbContext.Entry(op).State = EntityState.Detached;

			return op.Id.ToString();
		}
		
		// ---

		public async Task Update(string oplogId, UserOpLogStatus status, string message) {
			if (oplogId != null && long.TryParse(oplogId, out long id)) {

				var op = await (
					from s in _dbContext.UserOpLog
					where s.Id == id
					select s
				)
					.AsTracking()
					.FirstAsync()
				;
				if (op != null) {
					op.Status = status; // will be saved in the following f-n
					await CreateEntry(op.UserId, message, id, status);
					_dbContext.Entry(op).State = EntityState.Detached;
				}
			}
		}

		public async Task<string> NewGoldBuyingRequestForCryptoasset(long userId, EthereumToken ethereumToken, string destAddress, FiatCurrency fiatCurrency, long inputRate, long goldRate, string promoCode) {
			return await CreateEntry(userId, $"New GOLD buying #? for { ethereumToken.ToString() } requested to address { TextFormatter.MaskBlockchainAddress(destAddress) }; asset rate { TextFormatter.FormatAmount(inputRate, fiatCurrency) }, gold rate { TextFormatter.FormatAmount(goldRate, fiatCurrency) }; promo { (string.IsNullOrWhiteSpace(promoCode)? "-": promoCode) }");
		}

		public async Task<string> NewGoldSellingRequestForCryptoasset(long userId, EthereumToken ethereumToken, string destAddress, FiatCurrency fiatCurrency, long outputRate, long goldRate) { 
			return await CreateEntry(userId, $"New GOLD selling #? for { ethereumToken.ToString() } requested to address { TextFormatter.MaskBlockchainAddress(destAddress) }; asset rate { TextFormatter.FormatAmount(outputRate, fiatCurrency) }, gold rate { TextFormatter.FormatAmount(goldRate, fiatCurrency) }");
		}

		public async Task<string> NewGoldTransfer(long userId, string ethAddress, BigInteger goldAmount) {
			return await CreateEntry(userId, $"New gold transfer #? of {TextFormatter.FormatTokenAmount(goldAmount, TokensPrecision.EthereumGold)} oz requested from HW to {TextFormatter.MaskBlockchainAddress(ethAddress)}");
		}

		public async Task<string> NewCardVerification(long userId, long cardId, long centsAmount, FiatCurrency fiatCurrency) {
			return await CreateEntry(userId, $"New card #{ cardId } verification started with {TextFormatter.FormatAmount(centsAmount, fiatCurrency)}");
		}

		public async Task<string> NewGoldBuyingRequestWithCreditCard(long userId, string destAddress, FiatCurrency fiatCurrency, long goldRate, long centsAmount, string promoCode) {
			return await CreateEntry(userId, $"New GOLD buying #? of { TextFormatter.FormatAmount(centsAmount, fiatCurrency) } requested to address { TextFormatter.MaskBlockchainAddress(destAddress) }; gold rate { TextFormatter.FormatAmount(goldRate, fiatCurrency) }; promo { (string.IsNullOrWhiteSpace(promoCode)? "-": promoCode) }");
		}

		public async Task<string> NewGoldSellingRequestWithCreditCard(long userId, string destAddress, FiatCurrency fiatCurrency, long goldRate) {
			return await CreateEntry(userId, $"New GOLD selling #? requested to address { TextFormatter.MaskBlockchainAddress(destAddress) }; gold rate { TextFormatter.FormatAmount(goldRate, fiatCurrency) }");
		}

	}
}
