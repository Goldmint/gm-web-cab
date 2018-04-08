using Goldmint.Common;
using NLog;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Goldmint.CoreLogic.Services.Ticket.Impl {

	public class DBTicketDesk : ITicketDesk {

		// TODO: use logger

		private readonly DAL.ApplicationDbContext _dbContext;
		private ILogger _logger; 

		public DBTicketDesk(DAL.ApplicationDbContext dbContext, LogFactory logFactory) {
			_dbContext = dbContext;
			_logger = logFactory.GetLoggerFor(this);
		}

		// ---

		private async Task<string> CreateTicket(long userId, string message, long? refId = null, UserOpLogStatus status = UserOpLogStatus.Pending) {
			var op = new DAL.Models.UserOpLog() {
				Status = status,
				UserId = userId,
				RefId = refId,
				Message = message.LimitLength(DAL.Models.FieldMaxLength.Comment),
				TimeCreated = DateTime.UtcNow,
			};
			_dbContext.UserOpLog.Add(op);
			await _dbContext.SaveChangesAsync();
			_dbContext.Entry(op).State = EntityState.Detached;

			return op.Id.ToString();
		}
		
		// ---

		public async Task UpdateTicket(string ticketId, UserOpLogStatus status, string message) {
			if (ticketId != null && long.TryParse(ticketId, out long id)) {

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
					await CreateTicket(op.UserId, message, id, status);
					_dbContext.Entry(op).State = EntityState.Detached;
				}
			}
		}

		public async Task<string> NewGoldBuyingRequestForCryptoasset(long userId, CryptoCurrency cryptoCurrency, string destAddress, FiatCurrency fiatCurrency, long inputRate, long goldRate) {
			return await CreateTicket(userId, $"New gold buying #? for { cryptoCurrency.ToString() } to requested to address { TextFormatter.MaskBlockchainAddress(destAddress) }; asset rate { TextFormatter.FormatAmount(inputRate, fiatCurrency) }, gold rate { TextFormatter.FormatAmount(goldRate, fiatCurrency) }");
		}

		public async Task<string> NewGoldTransfer(long userId, string ethAddress, BigInteger goldAmount) {
			return await CreateTicket(userId, $"New gold transfer #? of {TextFormatter.FormatTokenAmount(goldAmount, Tokens.GOLD.Decimals)} oz requested from HW to {TextFormatter.MaskBlockchainAddress(ethAddress)}");
		}



		/*
		public async Task<string> NewCardVerification(DAL.Models.Identity.User user, Card card) {
			return await CreateTicket(user.Id, $"New card #{card.Id} verification started with {TextFormatter.FormatAmount(card.VerificationAmountCents, FiatCurrency.USD)}");
		}

		public async Task<string> NewCardDeposit(DAL.Models.Identity.User user, Card card, FiatCurrency currency, long amount) {
			return await CreateTicket(user.Id, $"New card deposit #? (card #{card.Id}, {card.CardMask}) started with {TextFormatter.FormatAmount(amount, currency)}");
		}

		public async Task<string> NewCardWithdraw(DAL.Models.Identity.User user, Card card, FiatCurrency currency, long amount) {
			return await CreateTicket(user.Id, $"New card witdraw #? (card #{card.Id}, {card.CardMask}) started with {TextFormatter.FormatAmount(amount, currency)}");
		}


		public async Task<string> NewSwiftDeposit(DAL.Models.Identity.User user, FiatCurrency currency, long amount) {
			return await CreateTicket(user.Id, $"New SWIFT deposit #? requested with {TextFormatter.FormatAmount(amount, currency)}");
		}

		public async Task<string> NewSwiftWithdraw(DAL.Models.Identity.User user, FiatCurrency currency, long amount) {
			return await CreateTicket(user.Id, $"New SWIFT withdraw #? requested with {TextFormatter.FormatAmount(amount, currency)}");
		}


		public async Task<string> NewCryptoDeposit(DAL.Models.Identity.User user, CryptoExchangeAsset asset, string address, FiatCurrency currency, long tokenRate) {
			return await CreateTicket(user.Id, $"New {asset.ToString()}-deposit #? requested from {TextFormatter.MaskBlockchainAddress(address)} at rate {TextFormatter.FormatAmount(tokenRate, currency)} per token");
		}

		public async Task<string> NewCryptoWithdraw(DAL.Models.Identity.User user, CryptoExchangeRequestOrigin origin, string address, FiatCurrency currency, long amount) {
			return await CreateTicket(user.Id, $"New {origin.ToString()}-witdraw #? to {TextFormatter.MaskBlockchainAddress(address)} started with {TextFormatter.FormatAmount(amount, currency)}");
		}


		public async Task<string> NewGoldBuying(DAL.Models.Identity.User user, string ethAddressOrNull, FiatCurrency currency, long fiatAmount, long rate, BigInteger mntpBalance, BigInteger estimatedGoldAmount, long feeCents) {
			return await CreateTicket(user.Id, $"New gold buying #? for {TextFormatter.FormatAmount(fiatAmount, currency)} requested from {( ethAddressOrNull != null? TextFormatter.MaskBlockchainAddress(ethAddressOrNull): "HW" )} at rate {TextFormatter.FormatAmount(rate, currency)}, {CoreLogic.Finance.Tokens.MntpToken.FromWei(mntpBalance)} mints, est. {CoreLogic.Finance.Tokens.GoldToken.FromWei(estimatedGoldAmount)} oz, fee {TextFormatter.FormatAmount(feeCents, currency)}");
		}

		public async Task<string> NewGoldSelling(DAL.Models.Identity.User user, string ethAddressOrNull, FiatCurrency currency, BigInteger goldAmount, long rate, BigInteger mntpBalance, long estimatedFiatAmount, long feeCents) {
			return await CreateTicket(user.Id, $"New gold selling #? of {CoreLogic.Finance.Tokens.GoldToken.FromWei(goldAmount)} oz requested from {( ethAddressOrNull != null? TextFormatter.MaskBlockchainAddress(ethAddressOrNull): "HW")} at rate {TextFormatter.FormatAmount(rate, currency)}, {CoreLogic.Finance.Tokens.MntpToken.FromWei(mntpBalance)} mints, est. {TextFormatter.FormatAmount(estimatedFiatAmount, currency)}, fee {TextFormatter.FormatAmount(feeCents, currency)}");
		}

		public async Task<string> NewGoldTransfer(DAL.Models.Identity.User user, string ethAddress, BigInteger goldAmount) {
			return await CreateTicket(user.Id, $"New gold transfer #? of {CoreLogic.Finance.Tokens.GoldToken.FromWei(goldAmount)} oz requested from HW to {TextFormatter.MaskBlockchainAddress(ethAddress)}");
		}
		*/

	}
}
