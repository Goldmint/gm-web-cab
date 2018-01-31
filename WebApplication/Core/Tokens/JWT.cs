using Goldmint.Common;
using Goldmint.DAL;
using Goldmint.DAL.Models.Identity;
using Goldmint.WebApplication.Core.Response;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Goldmint.WebApplication.Core.Tokens {

	public static class JWT {

		public const string GMAreaField = "gm_area";
		public const string GMIdField = "gm_id";
		public const string GMSecurityStampField = "gm_sstamp";
		public const string GMRoleField = "gm_role";

		// ---

		/// <summary>
		/// Default token validation parameters
		/// </summary>
		public static TokenValidationParameters ValidationParameters(AppConfig appConfig) {

			return new TokenValidationParameters() {
				NameClaimType = GMIdField,
				RoleClaimType = GMRoleField,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = CreateJwtKey(appConfig),
				ValidateIssuer = true,
				ValidIssuer = appConfig.Auth.JWT.Issuer,
				ValidateAudience = true,
				ValidAudiences = new[] { appConfig.Auth.JWT.AppAudience },
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero,
			};
		}

		/// <summary>
		/// Unique JWT subject
		/// </summary>
		private static string UniqueId(string salt) {
			return Hash.SHA256(Guid.NewGuid().ToString() + salt);
		}

		/// <summary>
		/// Get user's security stamp from DB and hash it
		/// </summary>
		private static string ObtainSecurityStamp(string input) {
			return Hash.SHA256(Hash.SHA256(input));
		}

		// ---

		/// <summary>
		/// Make a secure key from main secret phrase
		/// </summary>
		public static SymmetricSecurityKey CreateJwtKey(AppConfig appConfig) {
			return new SymmetricSecurityKey(
				System.Text.Encoding.UTF8.GetBytes(appConfig.Auth.JWT.Secret)
			);
		}

		/// <summary>
		/// Make a token for specified user with specified state
		/// </summary>
		public static string CreateAuthToken(AppConfig appConfig, User user, JwtArea area) {

			var now = DateTime.UtcNow;
			var uniqueness = UniqueId(appConfig.Auth.JWT.Secret);

			var claims = new[] {

				// jw main fields
				new Claim(JwtRegisteredClaimNames.Sub, uniqueness),
				new Claim(JwtRegisteredClaimNames.Jti, uniqueness),
				new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
				
				// gm fields
				new Claim(GMSecurityStampField, ObtainSecurityStamp(user.AccessStamp)),
				new Claim(GMIdField, user.UserName),
				new Claim(GMRoleField, "user"), // todo: implement roles
				new Claim(GMAreaField, area.ToString().ToLowerInvariant()),
			};

			var claimIdentity = new ClaimsIdentity(
				claims,
				JwtBearerDefaults.AuthenticationScheme
			);

			var creds = new SigningCredentials(CreateJwtKey(appConfig), SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: appConfig.Auth.JWT.Issuer,
				audience: appConfig.Auth.JWT.AppAudience,
				claims: claimIdentity.Claims,
				signingCredentials: creds,
				expires: now.AddSeconds(appConfig.Auth.JWT.ExpirationSec)
			);

			return (new JwtSecurityTokenHandler()).WriteToken(token);
		}

		/// <summary>
		/// Make a security token
		/// </summary>
		public static string CreateSecurityToken(AppConfig appConfig, string id, string securityStamp, JwtArea area, TimeSpan validFor, IEnumerable<Claim> optClaims = null) {

			var now = DateTime.UtcNow;
			var uniqueness = UniqueId(appConfig.Auth.JWT.Secret);

			var claims = new List<Claim>() {

				// jw main fields
				new Claim(JwtRegisteredClaimNames.Sub, uniqueness),
				new Claim(JwtRegisteredClaimNames.Jti, uniqueness),
				new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
				
				// gm fields
				new Claim(GMIdField, id),
				new Claim(GMSecurityStampField, ObtainSecurityStamp(securityStamp)),
				new Claim(GMAreaField, area.ToString().ToLowerInvariant()),
			};

			if (optClaims != null) {
				claims.AddRange(optClaims);
			}

			var creds = new SigningCredentials(CreateJwtKey(appConfig), SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: appConfig.Auth.JWT.Issuer,
				audience: appConfig.Auth.JWT.AppAudience,
				claims: claims,
				signingCredentials: creds,
				expires: now.Add(validFor)
			);

			return (new JwtSecurityTokenHandler()).WriteToken(token);
		}

		// ---

		public static JwtBearerEvents AddEvents() {

			return new JwtBearerEvents() {

				// user must get new token
				OnChallenge = (ctx) => {
					ctx.Response.StatusCode = 403;
					ctx.HandleResponse();
					return Task.CompletedTask;
				},

				/*OnMessageReceived = async (ctx) => {
					//var resp = Core.APIResponse.BadRequest(Core.APIErrorCode.Unauthorized);
					//await resp.WriteResponse(ctx.HttpContext).ConfigureAwait(false);
				},*/

				OnTokenValidated = async (ctx) => {
					var token = ctx.SecurityToken as JwtSecurityToken;
					try {

						// get passed username and stamp
						var userName = token.Claims.FirstOrDefault((c) => c.Type == GMIdField).Value;
						var userStamp = token.Claims.FirstOrDefault((c) => c.Type == GMSecurityStampField).Value;
						if (userName == null) {
							throw new Exception("JWT doesnt contain username");
						}

						// get security stamp of the user
						var dbContext = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
						var sstamp = await (
							from u in dbContext.Users
							where u.UserName == userName
							select ObtainSecurityStamp(u.AccessStamp)
						)
							.AsNoTracking()
							.FirstOrDefaultAsync()
						;

						// compare
						if (sstamp == null || userStamp == null || sstamp != userStamp) {
							throw new Exception("JWT failed to validate sstamp");
						}

						ctx.Success();
					}
					catch (Exception e) {
						ctx.Fail(e);
					}
				},

				/*OnAuthenticationFailed = async (ctx) => {
					var resp = APIResponse.BadRequest(APIErrorCode.Unauthorized);
					await resp.WriteResponse(ctx.HttpContext).ConfigureAwait(false);
					ctx.NoResult();
				},*/
			};
		}

		// ---

		public static async Task<bool> IsValid(AppConfig appConfig, string jwtToken, JwtArea expectedArea, Func<JwtSecurityToken, string, Task<string>> validStamp) {
			try {

				JwtSecurityToken token = null;
				{
					SecurityToken validatedToken = null;
					JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
					new JwtSecurityTokenHandler().ValidateToken(jwtToken, ValidationParameters(appConfig), out validatedToken);

					token = validatedToken as JwtSecurityToken;
					if (token == null) {
						return false;
					}
				}

				// check id
				var id = token.Claims.FirstOrDefault(_ => _.Type == GMIdField).Value;
				if (string.IsNullOrWhiteSpace(id)) {
					return false;
				}

				// check area
				var area = token.Claims.FirstOrDefault(_ => _.Type == GMAreaField).Value;
				if (area != expectedArea.ToString().ToLowerInvariant()) {
					return false;
				}

				// check security stamp
				if (validStamp != null) {
					var valid = await validStamp(token, id);
					if (valid == null) {
						return false;
					}

					var sstamp = token.Claims.FirstOrDefault((c) => c.Type == GMSecurityStampField).Value;
					if (sstamp != ObtainSecurityStamp(valid)) {
						return false;
					}
				}

				return true;

			} catch { }

			return false;
		}
	}
}
