using Goldmint.Common;
using Goldmint.CoreLogic.Services.Mutex.Impl;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.TransferGoldModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/gold/transfer")]
	public class TransferGoldController : BaseController {

		/// <summary>
		/// Transferring request of GOLD to eth address (hot wallet)
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("gold/hw/transfer")]
		[ProducesResponseType(typeof(HwTransferView), 200)]
		public async Task<APIResponse> HwTransfer([FromBody] HwTransferModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			if (!BigInteger.TryParse(model.Amount, out var amountWei) || amountWei < 1 || amountWei.ToString().Length > DAL.Models.FieldMaxLength.BlockchainCurrencyAmount) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			// ---

			var rcfg = RuntimeConfigHolder.Clone();
			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			if (!Core.Tokens.GoogleAuthenticator.Validate(model.TfaCode, user.TfaSecret)) {
				return APIResponse.BadRequest(nameof(model.TfaCode), "Invalid code");
			}

			if (await CoreLogic.User.HasPendingBlockchainOps(HttpContext.RequestServices, user.Id)) {
				return APIResponse.BadRequest(APIErrorCode.AccountPendingBlockchainOperation);
			}

			// ---

			var goldBalance = await EthereumObserver.GetHotWalletGoldBalance(user.UserName);
			if (amountWei > goldBalance) {
				return APIResponse.BadRequest(nameof(model.Amount), "Invalid amount");
			}

			var mutexBuilder =
				new MutexBuilder(MutexHolder)
				.Mutex(MutexEntity.UserHwOperation, user.Id)
			;

			// get into mutex
			return await mutexBuilder.CriticalSection(async (ok) => {
				if (ok) {

					var opLastTime = user.UserOptions.HotWalletTransferLastTime;

					// check rate
					if (opLastTime != null && (DateTime.UtcNow - opLastTime) < TimeSpan.FromSeconds(rcfg.Gold.Timeouts.HwUserOperationDelay)) {
						// failed
						return APIResponse.BadRequest(APIErrorCode.RateLimit);
					}

					var timeNow = DateTime.UtcNow;
					var ticket = await OplogProvider.NewGoldTransfer(user.Id, model.EthAddress, amountWei);

					// history
					var finHistory = new DAL.Models.UserFinHistory() {

						Status = UserFinHistoryStatus.Unconfirmed,
						Type = UserFinHistoryType.HwTransfer,

						Source = "HW",
						SourceAmount = TextFormatter.FormatTokenAmountFixed(amountWei, Tokens.GOLD.Decimals) + " GOLD",
						Destination = TextFormatter.MaskBlockchainAddress(model.EthAddress),
						DestinationAmount = null,
						Comment = "", // see below

						OplogId = ticket,
						TimeCreated = timeNow,
						UserId = user.Id,
					};

					// save
					DbContext.UserFinHistory.Add(finHistory);
					await DbContext.SaveChangesAsync();

					// operation
					var request = new DAL.Models.EthereumOperation() {

						Type = EthereumOperationType.TransferGoldFromHw,
						Status = EthereumOperationStatus.Prepared,

						DestinationAddress = model.EthAddress,
						Rate = "0",
						GoldAmount = amountWei.ToString(),
						OplogId = ticket,
						TimeCreated = timeNow,
						TimeNextCheck = timeNow,

						UserId = user.Id,
						RefUserFinHistoryId = finHistory.Id,
					};

					// save
					finHistory.Status = UserFinHistoryStatus.Processing;
					DbContext.EthereumOperation.Add(request);
					await DbContext.SaveChangesAsync();

					// update comment
					finHistory.Comment = $"Transfer order #{ request.Id } of { TextFormatter.FormatTokenAmount(amountWei, Tokens.GOLD.Decimals) } from hot wallet to {TextFormatter.MaskBlockchainAddress(model.EthAddress)}";
					await DbContext.SaveChangesAsync();

					try {
						await OplogProvider.Update(ticket, UserOpLogStatus.Pending, $"Transfer request ID is #{request.Id}");
					}
					catch {
					}

					// TODO: email

					user.UserOptions.HotWalletTransferLastTime = DateTime.UtcNow;
					await DbContext.SaveChangesAsync();

					return APIResponse.Success(
						new HwTransferView() { }
					);
				}

				// failed
				return APIResponse.BadRequest(APIErrorCode.RateLimit);
			});
		}
	}

}