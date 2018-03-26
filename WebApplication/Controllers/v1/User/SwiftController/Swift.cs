using Goldmint.Common;
using Goldmint.WebApplication.Core.Policies;
using Goldmint.WebApplication.Core.Response;
using Goldmint.WebApplication.Models.API;
using Goldmint.WebApplication.Models.API.v1.User.SwiftModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Controllers.v1.User {

	[Route("api/v1/user/fiat/swift")]
	public partial class SwiftController : BaseController {

		/// <summary>
		/// Templates list
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpGet, Route("list")]
		[ProducesResponseType(typeof(ListView), 200)]
		public async Task<APIResponse> List() {

			var user = await GetUserFromDb();
			var templates = await (
				from t in DbContext.SwiftTemplate
				where t.UserId == user.Id
				select t
			)
				.AsNoTracking()
				.ToListAsync()
			;

			var list = new List<ListView.Item>();
			foreach (var c in templates) {
				list.Add(new ListView.Item() {
					TemplateId = c.Id,
					Name = c.Name,
					Holder = c.Holder,
					Iban = c.Iban,
					Bic = c.Bic,
					Bank = c.Bank,
					Details = c.Details,
				});
			}

			return APIResponse.Success(
				new ListView() {
					List = list.ToArray(),
				}
			);
		}

		/// <summary>
		/// Add template
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("add")]
		[ProducesResponseType(typeof(AddView), 200)]
		public async Task<APIResponse> Add([FromBody] AddModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var userTier = CoreLogic.User.GetTier(user);
			var agent = GetUserAgentInfo();

			if (userTier < UserTier.Tier2) {
				return APIResponse.BadRequest(APIErrorCode.AccountNotVerified);
			}

			// make new template
			var tmpl = new DAL.Models.SwiftTemplate() {
				Name = model.Name.LimitLength(64),
				Holder = model.Holder.LimitLength(256),
				Iban = model.Iban.LimitLength(256),
				Bank = model.Bank.LimitLength(256),
				Bic = model.Bic.LimitLength(128),
				Details = model.Details.LimitLength(1024),
				TimeCreated = DateTime.UtcNow,
				UserId = user.Id,
			};
			DbContext.SwiftTemplate.Add(tmpl);
			await DbContext.SaveChangesAsync();

			// activity
			await CoreLogic.User.SaveActivity(
				services: HttpContext.RequestServices,
				user: user,
				type: Common.UserActivityType.Swift,
				comment: $"New bank details filled: {model.Name}",
				ip: agent.Ip,
				agent: agent.Agent
			);

			return APIResponse.Success(
				new AddView() {
				}
			);
		}

		/// <summary>
		/// Remove template by ID
		/// </summary>
		[RequireJWTAudience(JwtAudience.Cabinet), RequireJWTArea(JwtArea.Authorized), RequireAccessRights(AccessRights.Client)]
		[HttpPost, Route("remove")]
		[ProducesResponseType(typeof(RemoveView), 200)]
		public async Task<APIResponse> Remove([FromBody] RemoveModel model) {

			// validate
			if (BaseValidableModel.IsInvalid(model, out var errFields)) {
				return APIResponse.BadRequest(errFields);
			}

			var user = await GetUserFromDb();
			var agent = GetUserAgentInfo();

			// get the template
			var template = await (
					from t in DbContext.SwiftTemplate
					where t.UserId == user.Id && t.Id == model.TemplateId
					select t
				)
				.FirstOrDefaultAsync()
			;

			if (template != null) {

				DbContext.SwiftTemplate.Remove(template);
				if (await DbContext.SaveChangesAsync() > 0) {

					// activity
					await CoreLogic.User.SaveActivity(
						services: HttpContext.RequestServices,
						user: user,
						type: Common.UserActivityType.Swift,
						comment: $"Bank details deleted: {template.Name}",
						ip: agent.Ip,
						agent: agent.Agent
					);
				}

				return APIResponse.Success(
					new RemoveView()
				);
			}

			return APIResponse.BadRequest(nameof(model.TemplateId), "Invalid ID");
		}
		
	}
}
