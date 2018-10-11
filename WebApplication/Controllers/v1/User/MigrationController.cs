using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Goldmint.Common;
using Goldmint.DAL.Extensions;
using Goldmint.DAL.Models;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Microsoft.AspNetCore.Mvc;

namespace Goldmint.WebApplication.Controllers.v1.User
{
	[Route("api/v1/user/migration")]
	public class MigrationController : BaseController
	{
		/// <summary>
		/// MNTP token migration Ethereum to Sumus
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("mint/sumus")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> MintToSumus([FromBody] Models.API.v1.User.MigrationController.EthSumModel model)
		{

			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			if (await GetUserTier() < UserTier.Tier2)
			{
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
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
					TimeNextCheck = DateTime.UtcNow,
					User = await GetUserFromDb()
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
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("mint/ethereum")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> MintToEthereum([FromBody] Models.API.v1.User.MigrationController.SumEthModel model)
		{

			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			if (await GetUserTier() < UserTier.Tier2)
			{
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
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
					TimeNextCheck = DateTime.UtcNow.Add(TimeSpan.FromSeconds(AppConfig.Services.Sumus.MigrationRequestNextCheckDelay)),
					User = await GetUserFromDb()
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
        //[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
        [HttpPost, Route("gold/sumus")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> GoldToSumus([FromBody] Models.API.v1.User.MigrationController.EthSumModel model)
		{

			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			if (await GetUserTier() < UserTier.Tier2)
			{
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
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
					TimeNextCheck = DateTime.UtcNow,
					User = await GetUserFromDb()
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
        //[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
        [HttpPost, Route("gold/ethereum")]
		[ProducesResponseType(typeof(object), 200)]
		public async Task<APIResponse> GoldToEthereum([FromBody] Models.API.v1.User.MigrationController.SumEthModel model)
		{
			if (BaseValidableModel.IsInvalid(model, out var errFields))
			{
				return APIResponse.BadRequest(errFields);
			}

			if (await GetUserTier() < UserTier.Tier2)
			{
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
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
					TimeNextCheck = DateTime.UtcNow.Add(
						TimeSpan.FromSeconds(AppConfig.Services.Sumus.MigrationRequestNextCheckDelay)),
					User = await GetUserFromDb()
				});

				await DbContext.SaveChangesAsync();
			}
			catch (Exception e) when (e.IsMySqlDuplicateException()) {
				return APIResponse.BadRequest(APIErrorCode.MigrationDuplicateRequest);
			}
			catch (Exception e) {
				return APIResponse.GeneralInternalFailure(e);
			}
			return APIResponse.Success();
		}

	    /// <summary>
	    /// Get contract info
	    /// </summary>
        //[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
        [HttpGet, Route("status")]
	    [ProducesResponseType(typeof(object), 200)]
	    public async Task<APIResponse> GetMigrationInfo()
	    {
	        if (await GetUserTier() < UserTier.Tier2)
	        {
	            return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
	        }

            return APIResponse.Success(
	            new StatusView()
	            {
	                Ethereum = new StatusView.EthereumData()
	                {
	                    GoldToken = AppConfig.Services.Ethereum.GoldContractAddress,
	                    MintToken = AppConfig.Services.Ethereum.MntpContractAddress,
	                    MigrationAddress = AppConfig.Services.Ethereum.MigrationContractAddress,
	                },
	                Sumus = new StatusView.SumusData()
	                {
	                    MigrationAddress = AppConfig.Services.Sumus.MigrationHolderAddress,
	                },
	            }
	        );
	    }


	    internal class StatusView
	    {

	        /// <summary>
	        /// Ethereum data
	        /// </summary>
	        [Required]
	        public EthereumData Ethereum { get; set; }

	        /// <summary>
	        /// Sumus data
	        /// </summary>
	        [Required]
	        public SumusData Sumus { get; set; }

	        // ---

	        public class EthereumData
	        {

	            /// <summary>
	            /// Gold token address
	            /// </summary>
	            public string GoldToken { get; set; }

	            /// <summary>
	            /// Mint token address
	            /// </summary>
	            public string MintToken { get; set; }

	            /// <summary>
	            /// Migration contract address
	            /// </summary>
	            public string MigrationAddress { get; set; }
	        }

	        public class SumusData
	        {

	            /// <summary>
	            /// Migration address
	            /// </summary>
	            public string MigrationAddress { get; set; }
	        }
	    }

    }
}
