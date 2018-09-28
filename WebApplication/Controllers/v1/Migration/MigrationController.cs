using System;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.DAL.Extensions;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace Goldmint.WebApplication.Controllers.v1.Migration
{

	[Route("api/v1/migration")]
	//[ApiController]
	public class MigrationController : BaseController
	{

		// TODO: move to AppConfig
		public static TimeSpan SumusNextCheckDelay = TimeSpan.FromSeconds(20);

		/// <summary>
		/// MNTP token migration Ethereum to Sumus
		/// </summary>
		[HttpPost, Route("mint/sumus")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> MintToSumus([FromBody] Models.API.v1.Migration.MigrationController.EthSumModel model)
		{

			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			try
			{
			    DbContext.MigrationEthereumToSumusRequest.Add(new MigrationEthereumToSumusRequest
			    {
					Asset = MigrationRequestAsset.Mnt,
					Status = MigrationRequestStatus.TransferConfirmation,
					EthAddress = model.EthereumAddress,
					SumAddress = model.SumusAddress,
					Block = 0,
					TimeCreated = DateTime.UtcNow,
					TimeNextCheck = DateTime.UtcNow
				});

				await DbContext.SaveChangesAsync();
			}
			catch (Exception e) when (e.IsMySqlDuplicateException())
			{
				return APIResponse.BadRequest(APIErrorCode.MigrationDuplicateRequest);
			}
			catch (Exception e)
			{
				return APIResponse.GeneralInternalFailure(e);
			}

			return APIResponse.Success();
		}

		/// <summary>
		/// MNTP token migration Sumus to Ethereum
		/// </summary>
		[HttpPost, Route("mint/ethereum")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> MintToEthereum([FromBody] Models.API.v1.Migration.MigrationController.SumEthModel model)
		{

			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			try
			{
			    DbContext.MigrationSumusToEthereumRequest.Add(new MigrationSumusToEthereumRequest
			    {
					Asset = MigrationRequestAsset.Mnt,
					Status = MigrationRequestStatus.TransferConfirmation,
					SumAddress = model.SumusAddress,
					EthAddress = model.EthereumAddress,
					Block = 0,
					TimeCreated = DateTime.UtcNow,
					TimeNextCheck = DateTime.UtcNow.Add(SumusNextCheckDelay)
				});

				await DbContext.SaveChangesAsync();
			}
			catch (Exception e) when (e.IsMySqlDuplicateException())
			{
				return APIResponse.BadRequest(APIErrorCode.MigrationDuplicateRequest);
			}
			catch (Exception e)
			{
				return APIResponse.GeneralInternalFailure(e);
			}

			return APIResponse.Success();
		}

		/// <summary>
		/// GOLD token migration Ethereum to Sumus
		/// </summary>
		[HttpPost, Route("gold/sumus")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> GoldToSumus([FromBody] Models.API.v1.Migration.MigrationController.EthSumModel model)
		{

			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			try
			{
			    DbContext.MigrationEthereumToSumusRequest.Add(new MigrationEthereumToSumusRequest
			    {
					Asset = MigrationRequestAsset.Gold,
					Status = MigrationRequestStatus.TransferConfirmation,
					EthAddress = model.EthereumAddress,
					SumAddress = model.SumusAddress,
					Block = 0,
					TimeCreated = DateTime.UtcNow,
					TimeNextCheck = DateTime.UtcNow
				});

				await DbContext.SaveChangesAsync();
			}
			catch (Exception e) when (e.IsMySqlDuplicateException())
			{
				return APIResponse.BadRequest(APIErrorCode.MigrationDuplicateRequest);
			}
			catch (Exception e)
			{
				return APIResponse.GeneralInternalFailure(e);
			}

			return APIResponse.Success();
		}

		/// <summary>
		/// GOLD token migration Sumus to Ethereum
		/// </summary>
		[HttpPost, Route("gold/ethereum")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> GoldToEthereum([FromBody] Models.API.v1.Migration.MigrationController.SumEthModel model)
		{

			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			try
			{
			    DbContext.MigrationSumusToEthereumRequest.Add(new MigrationSumusToEthereumRequest
			    {
					Asset = MigrationRequestAsset.Gold,
					Status = MigrationRequestStatus.TransferConfirmation,
					SumAddress = model.SumusAddress,
					EthAddress = model.EthereumAddress,
					Block = 0,
					TimeCreated = DateTime.UtcNow,
					TimeNextCheck = DateTime.UtcNow.Add(SumusNextCheckDelay)
				});

				await DbContext.SaveChangesAsync();
			}
			catch (Exception e) when (e.IsMySqlDuplicateException())
			{
				return APIResponse.BadRequest(APIErrorCode.MigrationDuplicateRequest);
			}
			catch (Exception e)
			{
				return APIResponse.GeneralInternalFailure(e);
			}

			return APIResponse.Success();
		}
	}
}
