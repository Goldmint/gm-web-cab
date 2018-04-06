using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.BuyGoldModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	public partial class BuyGoldController : BaseController {

		/// <summary>
		/// ETH => GOLD
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("asset/eth")]
		[ProducesResponseType(typeof(ForAssetEthView), 200)]
		public async Task<APIResponse> ForAssetEth([FromBody] ForAssetEthModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			if (!BigInteger.TryParse(model.Amount, out var amountWei) || amountWei.ToString().Length > 64 || amountWei < 1) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// ---

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier1) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// ---

			var currency = FiatCurrency.USD;

			// TODO: apply GM fee
			var ethRate = await CryptoassetRateProvider.GetRate(CryptoCurrency.ETH, currency);
			var goldRate = await GoldRateProvider.GetRate(currency);

			var timeNow = DateTime.UtcNow;
			var timeExpires = timeNow.AddSeconds(AppConfig.Constants.TimeLimits.BuyGoldForEthRequestTimeoutSec);

			var ticket = await TicketDesk.NewGoldBuyingRequestForCryptoasset(
				userId: user.Id,
				cryptoCurrency: CryptoCurrency.ETH,
				destAddress: model.EthAddress,
				fiatCurrency: currency,
				inputRate: ethRate,
				goldRate: goldRate
			);

			// history
			var finHistory = new DAL.Models.UserFinHistory() {

				Status = UserFinHistoryStatus.Unconfirmed,
				Type = UserFinHistoryType.GoldBuy,
				Source = "ETH", Destination = "GOLD",
				Comment = "", // see below

				DeskTicketId = ticket,
				TimeCreated = timeNow,
				TimeExpires = timeExpires,
				UserId = user.Id,
			};

			// add and save
			DbContext.UserFinHistory.Add(finHistory);
			await DbContext.SaveChangesAsync();

			// request
			var request = new DAL.Models.BuyGoldRequest() {

				Status = BuyGoldRequestStatus.Unconfirmed,
				Input = BuyGoldRequestInput.EthereumDirectPayment,
				Destination = BuyGoldRequestDestination.EthereumAddress,
				DestinationAddress = model.EthAddress,

				ExchangeCurrency = currency,
				InputRateCents = ethRate,
				GoldRateCents = goldRate,
				
				DeskTicketId = ticket,
				TimeCreated = timeNow,
				TimeExpires = timeExpires,
				TimeNextCheck = timeNow,

				UserId = user.Id,
				RefUserFinHistoryId = finHistory.Id,
			};

			// add and save
			DbContext.BuyGoldRequest.Add(request);
			await DbContext.SaveChangesAsync();

			// update comment
			finHistory.Comment = $"Request #{request.Id}, {TextFormatter.FormatAmount(goldRate, currency)} per GOLD, {TextFormatter.FormatAmount(ethRate, currency)} per ETH,";
			await DbContext.SaveChangesAsync();

			// TODO: email notification?

			return APIResponse.Success(
				new ForAssetEthView() {
					RequestId = request.Id,
					EthRate = ethRate / 100d,
					GoldRate = goldRate / 100d,
					Currency = currency.ToString().ToUpper(),
					Expires = ((DateTimeOffset)request.TimeExpires).ToUnixTimeSeconds(),
				}
			);
		}

		/*
				// TODO: move to app settings constants
				private static readonly TimeSpan HWOperationTimeLimit = TimeSpan.FromMinutes(30);
				private static readonly TimeSpan ExchangeConfirmationTimeout = TimeSpan.FromMinutes(2);

				/// <summary>
				/// Confirm buying/selling request
				/// </summary>
				[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
				[HttpPost, Route("gold/confirm")]
				[ProducesResponseType(typeof(ConfirmView), 200)]
				public async Task<APIResponse> Confirm([FromBody] ConfirmModel model) {

					// validate
					if (BaseValidableModel.IsInvalid(model, out var errFields)) {
						return APIResponse.BadRequest(errFields);
					}

					var user = await GetUserFromDb();
					var agent = GetUserAgentInfo();

					// ---

					BuyRequest requestBuying = null;
					SellRequest requestSelling = null;

					if (model.IsBuying) {
						requestBuying = await (
							from r in DbContext.BuyRequest
							where
								r.Type == GoldExchangeRequestType.EthRequest &&
								r.Id == model.RequestId &&
								r.UserId == user.Id &&
								r.Status == GoldExchangeRequestStatus.Unconfirmed &&
								(DateTime.UtcNow - r.TimeCreated) <= ExchangeConfirmationTimeout
							select r
						)
						.Include(_ => _.RefFinancialHistory)
						.AsTracking()
						.FirstOrDefaultAsync()
						;
					}
					else {
						requestSelling = await (
							from r in DbContext.SellRequest
							where
								r.Type == GoldExchangeRequestType.EthRequest &&
								r.Id == model.RequestId &&
								r.UserId == user.Id &&
								r.Status == GoldExchangeRequestStatus.Unconfirmed &&
								(DateTime.UtcNow - r.TimeCreated) <= ExchangeConfirmationTimeout
							select r
						)
						.Include(_ => _.RefFinancialHistory)
						.AsTracking()
						.FirstOrDefaultAsync()
						;
					}

					// request exists
					if (requestSelling == null && requestBuying == null) {
						return APIResponse.BadRequest(nameof(model.RequestId), "Invalid request id");
					}

					// mark request for processing
					var deskTicketId = (string)null;
					if (requestBuying != null) {
						deskTicketId = requestBuying.DeskTicketId;

						requestBuying.RefFinancialHistory.Status = FinancialHistoryStatus.Manual;
						requestBuying.Status = GoldExchangeRequestStatus.Confirmed;
					}

					if (requestSelling != null) {
						deskTicketId = requestSelling.DeskTicketId;

						requestSelling.RefFinancialHistory.Status = FinancialHistoryStatus.Manual;
						requestSelling.Status = GoldExchangeRequestStatus.Confirmed;
					}

					await DbContext.SaveChangesAsync();

					try {
						await TicketDesk.UpdateTicket(deskTicketId, UserOpLogStatus.Pending, "Request has been confirmed by user. Awaiting for blockchain");
					}
					catch {
					}

					return APIResponse.Success(
						new ConfirmView() { }
					);
				}

				/// <summary>
				/// Confirm buying/selling request (hot wallet)
				/// </summary>
				[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
				[HttpPost, Route("gold/hw/confirm")]
				[ProducesResponseType(typeof(HWConfirmView), 200)]
				public async Task<APIResponse> HwConfirm([FromBody] HWConfirmModel model) {

					// validate
					if (BaseValidableModel.IsInvalid(model, out var errFields)) {
						return APIResponse.BadRequest(errFields);
					}

					var user = await GetUserFromDb();
					var agent = GetUserAgentInfo();

					// ---

					var mutexBuilder =
						new MutexBuilder(MutexHolder)
						.Mutex(MutexEntity.HWOperation, user.Id)
					;

					// get into mutex
					return await mutexBuilder.CriticalSection(async (ok) => {
						if (ok) {

							BuyRequest requestBuying = null;
							SellRequest requestSelling = null;
							DateTime? opLastTime = null;

							if (model.IsBuying) {
								requestBuying = await (
									from r in DbContext.BuyRequest
									where
										r.Type == GoldExchangeRequestType.HWRequest &&
										r.Id == model.RequestId &&
										r.UserId == user.Id &&
										r.Status == GoldExchangeRequestStatus.Unconfirmed &&
										(DateTime.UtcNow - r.TimeCreated) <= ExchangeConfirmationTimeout
									select r
								)
								.Include(_ => _.RefFinancialHistory)
								.AsTracking()
								.FirstOrDefaultAsync()
								;

								opLastTime = user.UserOptions.HotWalletBuyingLastTime;
							}
							else {
								requestSelling = await (
									from r in DbContext.SellRequest
									where
										r.Type == GoldExchangeRequestType.HWRequest &&
										r.Id == model.RequestId &&
										r.UserId == user.Id &&
										r.Status == GoldExchangeRequestStatus.Unconfirmed &&
										(DateTime.UtcNow - r.TimeCreated) <= ExchangeConfirmationTimeout
									select r
								)
								.Include(_ => _.RefFinancialHistory)
								.AsTracking()
								.FirstOrDefaultAsync()
								;

								opLastTime = user.UserOptions.HotWalletSellingLastTime;
							}

							// request exists
							if (requestSelling == null && requestBuying == null) {
								return APIResponse.BadRequest(nameof(model.RequestId), "Invalid request id");
							}

							// check rate
							if (opLastTime != null && (DateTime.UtcNow - opLastTime) < HWOperationTimeLimit) {
								// failed
								return APIResponse.BadRequest(APIErrorCode.RateLimit);
							}

							// mark request for processing
							var deskTicketId = (string) null;
							if (requestBuying != null) {
								deskTicketId = requestBuying.DeskTicketId;

								requestBuying.RefFinancialHistory.Status = FinancialHistoryStatus.Processing;
								requestBuying.Status = GoldExchangeRequestStatus.Prepared;
								requestBuying.TimeRequested = DateTime.UtcNow;
								requestBuying.TimeNextCheck = DateTime.UtcNow;

								user.UserOptions.HotWalletBuyingLastTime = DateTime.UtcNow;
							}

							if (requestSelling != null) {
								deskTicketId = requestSelling.DeskTicketId;

								requestSelling.RefFinancialHistory.Status = FinancialHistoryStatus.Processing;
								requestSelling.Status = GoldExchangeRequestStatus.Prepared;
								requestSelling.TimeRequested = DateTime.UtcNow;
								requestSelling.TimeNextCheck = DateTime.UtcNow;

								user.UserOptions.HotWalletSellingLastTime = DateTime.UtcNow;
							}

							await DbContext.SaveChangesAsync();

							try {
								await TicketDesk.UpdateTicket(deskTicketId, UserOpLogStatus.Pending, "Request has been confirmed by user");
							}
							catch {
							}

							return APIResponse.Success(
								new HWConfirmView() { }
							);
						}

						// failed
						return APIResponse.BadRequest(APIErrorCode.RateLimit);
					});
				}

				/// <summary>
				/// Transferring request of GOLD to eth address (hot wallet)
				/// </summary>
				[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
				[HttpPost, Route("gold/hw/transfer")]
				[ProducesResponseType(typeof(HWTransferView), 200)]
				public async Task<APIResponse> HwTransfer([FromBody] HWTransferModel model) {

					// validate
					if (BaseValidableModel.IsInvalid(model, out var errFields)) {
						return APIResponse.BadRequest(errFields);
					}

					if (!BigInteger.TryParse(model.Amount, out var amountWei) || amountWei < 1 || amountWei.ToString().Length > 64) {
						return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
					}

					// ---

					var user = await GetUserFromDb();
					var agent = GetUserAgentInfo();

					if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
						return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
					}

					// ---

					var goldBalance = await EthereumObserver.GetUserGoldBalance(user.UserName);
					if (amountWei > goldBalance) {
						return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
					}

					var mutexBuilder =
						new MutexBuilder(MutexHolder)
						.Mutex(MutexEntity.HWOperation, user.Id)
					;

					// get into mutex
					return await mutexBuilder.CriticalSection(async (ok) => {
						if (ok) {

							var opLastTime = user.UserOptions.HotWalletTransferLastTime;

							// check rate
							if (opLastTime != null && (DateTime.UtcNow - opLastTime) < HWOperationTimeLimit) {
								// failed
								return APIResponse.BadRequest(APIErrorCode.RateLimit);
							}

							var ticket = await TicketDesk.NewGoldTransfer(user, model.EthAddress, amountWei);

							// history
							var finHistory = new DAL.Models.FinancialHistory() {
								Status = FinancialHistoryStatus.Unconfirmed,
								Type = FinancialHistoryType.HwTransfer,
								AmountCents = (long)(CoreLogic.Finance.Tokens.GoldToken.FromWei(amountWei) * 100),
								FeeCents = 0,
								DeskTicketId = ticket,
								TimeCreated = DateTime.UtcNow,
								UserId = user.Id,
								Comment = "" // see below
							};

							// save
							DbContext.FinancialHistory.Add(finHistory);
							await DbContext.SaveChangesAsync();

							// request
							var request = new TransferRequest() {
								Status = GoldExchangeRequestStatus.Prepared,
								DestinationAddress = model.EthAddress,
								AmountWei = amountWei.ToString(),
								DeskTicketId = ticket,
								TimeCreated = DateTime.UtcNow,
								TimeNextCheck = DateTime.UtcNow,

								UserId = user.Id,
								RefFinancialHistoryId = finHistory.Id,
							};

							// save
							finHistory.Status = FinancialHistoryStatus.Processing;
							DbContext.TransferRequest.Add(request);
							await DbContext.SaveChangesAsync();

							// update comment
							finHistory.Comment = $"Transfer order #{request.Id} of {CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(amountWei)} GOLD from hot wallet to {TextFormatter.MaskBlockchainAddress(model.EthAddress)}";
							await DbContext.SaveChangesAsync();

							try {
								await TicketDesk.UpdateTicket(ticket, UserOpLogStatus.Pending, $"Transfer request ID is #{request.Id}");
							}
							catch {
							}

							// activity
							await CoreLogic.User.SaveActivity(
								services: HttpContext.RequestServices,
								user: user,
								type: Common.UserActivityType.Exchange,
								comment: $"GOLD transfer request #{request.Id} ({ CoreLogic.Finance.Tokens.GoldToken.FromWeiFixed(amountWei) } oz) from HW to {Common.TextFormatter.MaskBlockchainAddress(model.EthAddress)} initiated",
								ip: agent.Ip,
								agent: agent.Agent
							);

							user.UserOptions.HotWalletTransferLastTime = DateTime.UtcNow;
							await DbContext.SaveChangesAsync();

							return APIResponse.Success(
								new HWTransferView() { }
							);
						}

						// failed
						return APIResponse.BadRequest(APIErrorCode.RateLimit);
					});
				}
		*/
	}
}