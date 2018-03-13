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

		//public const string Issuer = "app.goldmint.io";

		public const string GMAreaField = "gm_area";
		public const string GMIdField = "gm_id";
		public const string GMSecurityStampField = "gm_sstamp";
		public const string GMRightsField = "gm_rights";

		// ---

		/// <summary>
		/// Default token validation parameters
		/// </summary>
		public static TokenValidationParameters ValidationParameters(AppConfig appConfig) {

			var auds = (from a in appConfig.Auth.Jwt.Audiences select a.Audience.ToLower()).ToArray();

			return new TokenValidationParameters() {
				NameClaimType = GMIdField,
				RoleClaimType = GMRightsField,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = CreateJwtSecurityKey(appConfig.Auth.Jwt.Secret),
				ValidateIssuer = true,
				ValidIssuer = appConfig.Auth.Jwt.Issuer,
				ValidateAudience = true,
				ValidAudiences = auds,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero,
			};
		}

		/// <summary>
		/// Unique JWT subject
		/// </summary>
		private static string UniqueId(string salt) {
			return Hash.SHA256(Guid.NewGuid().ToString("N") + salt);
		}

		/// <summary>
		/// Get user's security stamp from DB and hash it
		/// </summary>
		private static string ObtainSecurityStamp(string input) {
			return Hash.SHA256(Hash.SHA256(input));
		}

		/// <summary>
		/// Audience settings
		/// </summary>
		private static AppConfig.AuthSection.JwtSection.AudienceSection GetAudienceSettings(AppConfig appConfig, JwtAudience audience) {
			return (from a in appConfig.Auth.Jwt.Audiences where a.Audience == audience.ToString() select a).FirstOrDefault();
		}
		
		/// <summary>
		/// Make a secure key from main secret phrase
		/// </summary>
		public static SymmetricSecurityKey CreateJwtSecurityKey(string secret) {
			return new SymmetricSecurityKey(
				System.Text.Encoding.UTF8.GetBytes(
					secret
				)
			);
		}

		// ---

		/// <summary>
		/// Make a token for specified user with specified state
		/// </summary>
		public static string CreateAuthToken(AppConfig appConfig, JwtAudience audience, JwtArea area, User user, long rightsMask) {
			var now = DateTime.UtcNow;
			var uniqueness = UniqueId(appConfig.Auth.Jwt.Secret);
			var audienceSett = GetAudienceSettings(appConfig, audience);

			var claims = new[] {

				// jw main fields
				new Claim(JwtRegisteredClaimNames.Sub, uniqueness),
				new Claim(JwtRegisteredClaimNames.Jti, uniqueness),
				new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
				
				// gm fields
				new Claim(GMSecurityStampField, ObtainSecurityStamp(user.JWTSalt)),
				new Claim(GMIdField, user.UserName),
				new Claim(GMRightsField, rightsMask.ToString()),
				new Claim(GMAreaField, area.ToString().ToLower()),
			};

			var claimIdentity = new ClaimsIdentity(
				claims,
				JwtBearerDefaults.AuthenticationScheme
			);

			var creds = new SigningCredentials(
				CreateJwtSecurityKey(appConfig.Auth.Jwt.Secret), 
				SecurityAlgorithms.HmacSha256
			);

			var token = new JwtSecurityToken(
				issuer: appConfig.Auth.Jwt.Issuer,
				audience: audienceSett.Audience.ToLower(),
				claims: claimIdentity.Claims,
				signingCredentials: creds,
				expires: now.AddSeconds(audienceSett.ExpirationSec)
			);

			return (new JwtSecurityTokenHandler()).WriteToken(token);
		}

		/// <summary>
		/// Make a security token
		/// </summary>
		public static string CreateSecurityToken(AppConfig appConfig, JwtAudience audience, JwtArea area, string entityId, string securityStamp, TimeSpan validFor, IEnumerable<Claim> optClaims = null) {

			var now = DateTime.UtcNow;
			var uniqueness = UniqueId(appConfig.Auth.Jwt.Secret);
			var audienceSett = GetAudienceSettings(appConfig, audience);

			var claims = new List<Claim>() {

				// jw main fields
				new Claim(JwtRegisteredClaimNames.Sub, uniqueness),
				new Claim(JwtRegisteredClaimNames.Jti, uniqueness),
				new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
				
				// gm fields
				new Claim(GMIdField, entityId),
				new Claim(GMSecurityStampField, ObtainSecurityStamp(securityStamp)),
				new Claim(GMAreaField, area.ToString().ToLower()),
			};

			if (optClaims != null) {
				claims.AddRange(optClaims);
			}

			var creds = new SigningCredentials(
				CreateJwtSecurityKey(appConfig.Auth.Jwt.Secret), 
				SecurityAlgorithms.HmacSha256
			);

			var token = new JwtSecurityToken(
				issuer: appConfig.Auth.Jwt.Issuer,
				audience: audienceSett.Audience.ToLower(),
				claims: claims,
				signingCredentials: creds,
				expires: now.Add(validFor)
			);

			return (new JwtSecurityTokenHandler()).WriteToken(token);
		}

		/// <summary>
		/// Make a token for Zendesk SSO flow
		/// </summary>
		public static string CreateZendeskSsoToken(AppConfig appConfig, User user) {
			var now = DateTime.UtcNow;
			var uniqueness = UniqueId(appConfig.Auth.Jwt.Secret);

			var claims = new[] {
				new Claim(JwtRegisteredClaimNames.Jti, uniqueness),
				new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
				new Claim("email", user.NormalizedEmail.ToLower()),
				new Claim("name", user.UserName),
				new Claim("external_id", user.UserName + "@" + appConfig.Auth.Jwt.Issuer),
				new Claim("role", "user"),
			};

			var creds = new SigningCredentials(
				CreateJwtSecurityKey(appConfig.Auth.ZendeskSso.JwtSecret), 
				SecurityAlgorithms.HmacSha256
			);

			var token = new JwtSecurityToken(
				issuer: appConfig.Auth.Jwt.Issuer,
				claims: claims,
				signingCredentials: creds
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

				OnTokenValidated = async (ctx) => {
					var token = ctx.SecurityToken as JwtSecurityToken;
					try {

						if (token == null) {
							throw new Exception("JWT is null");
						}

						// get passed username and stamp
						var userName = token.Claims.FirstOrDefault((c) => c.Type == GMIdField)?.Value;
						var userStamp = token.Claims.FirstOrDefault((c) => c.Type == GMSecurityStampField)?.Value;
						if (userName == null) {
							throw new Exception("JWT doesnt contain username");
						}

						// get security stamp of the user
						var dbContext = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
						var sstamp = await (
							from u in dbContext.Users
							where u.UserName == userName
							select ObtainSecurityStamp(u.JWTSalt)
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
				}
			};
		}

		public static async Task<bool> IsValid(AppConfig appConfig, string jwtToken, JwtAudience? expectedAudience, JwtArea? expectedArea, Func<JwtSecurityToken, string, Task<string>> validStamp) {
			try {

				// base validation
				JwtSecurityToken token = null;
				{
					new JwtSecurityTokenHandler().ValidateToken(jwtToken, ValidationParameters(appConfig), out var validatedToken);
					token = validatedToken as JwtSecurityToken;
					if (token == null) {
						return false;
					}
				}

				// check id
				var id = token.Claims.FirstOrDefault(_ => _.Type == GMIdField)?.Value;
				if (string.IsNullOrWhiteSpace(id)) {
					return false;
				}

				// check audience
				if (expectedAudience != null) {
					var aud = token.Claims.FirstOrDefault(_ => _.Type == "aud")?.Value;
					if (aud != expectedAudience.ToString().ToLowerInvariant()) {
						return false;
					}
				}

				// check area
				if (expectedArea != null) {
					var area = token.Claims.FirstOrDefault(_ => _.Type == GMAreaField)?.Value;
					if (area != expectedArea.ToString().ToLowerInvariant()) {
						return false;
					}
				}

				// check security stamp
				if (validStamp != null) {
					var valid = await validStamp(token, id);
					if (valid == null) {
						return false;
					}

					var sstamp = token.Claims.FirstOrDefault((c) => c.Type == GMSecurityStampField)?.Value;
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
