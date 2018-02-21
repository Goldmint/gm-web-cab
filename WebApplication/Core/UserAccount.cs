using Goldmint.Common;
using Goldmint.DAL;
using Goldmint.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Goldmint.CoreLogic.Services.SignedDoc;
using Goldmint.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace Goldmint.WebApplication.Core {

	public static class UserAccount {

		
		/// <summary>
		/// New user account
		/// </summary>
		public static async Task<CreateUserAccountResult> CreateUserAccount(IServiceProvider services, string email, string password = null, bool emailConfirmed = false) {

			var logger = services.GetLoggerFor(typeof(UserAccount));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var userManager = services.GetRequiredService<UserManager<User>>();

			var ret = new CreateUserAccountResult() {
			};

			if (string.IsNullOrWhiteSpace(email)) {
				ret.IsEmailExists = true;
				return ret;
			}

			var tfaSecret = SecureRandom.GetString09azAZSpecs(14);

			try {

				var newUser = new User() {
					UserName = email,
					Email = email,
					TFASecret = tfaSecret,
					EmailConfirmed = emailConfirmed,
					AccessRights = 0,

					UserOptions = new DAL.Models.UserOptions() {
					},

					TimeRegistered = DateTime.UtcNow,
				};

				var result = (IdentityResult)null;
				if (password != null) {
					result = await userManager.CreateAsync(newUser, password);
				} else {
					result = await userManager.CreateAsync(newUser);
				}

				if (result.Succeeded) {
					ret.User = newUser;

					try {
						var name = string.Format("u{0:000000}", newUser.Id);

						newUser.UserName = name;
						newUser.NormalizedUserName = name.ToUpperInvariant();
						newUser.AccessStampWeb = GenerateAccessStamp();
						newUser.AccessRights = (long)AccessRights.Client;

						await dbContext.SaveChangesAsync();
					}
					catch { }
				}
				else {
					foreach (var v in result.Errors) {
						if (v.Code == "DuplicateUserName") {
							ret.IsUsernameExists = true;
						}
						else if (v.Code == "DuplicateEmail") {
							ret.IsEmailExists = true;
						}
						else {
							throw new Exception("Unexpected result error: " + v.Code);
						}
					}
				}
			} catch (Exception e) {
				logger?.Error(e, "Failed to create user account");
			}

			return ret;
		}

		/// <summary>
		/// Random access stamp
		/// </summary>
		public static string GenerateAccessStamp() {
			return SecureRandom.GetString09azAZ(64);
		}

		/// <summary>
		/// Reset current agreement and resend to specified email address
		/// </summary>
		public static async Task<bool> ResendVerificationPrimaryAgreement(IServiceProvider services, User user, string email) {

			if (user == null) {
				throw new ArgumentException("User is null");
			}
			if (user.UserVerification == null) {
				throw new ArgumentException("User verification not included");
			}
			if (user.UserVerification.FirstName == null || user.UserVerification.LastName == null) {
				throw new ArgumentException("User has no first/last name");
			}

			// ---

			var logger = services.GetLoggerFor(typeof(UserAccount));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var appConfig = services.GetRequiredService<AppConfig>();
			var docService = services.GetRequiredService<IDocSigningProvider>();

			// create new request
			var request = new SignedDocument() {
				Type = SignedDocumentType.PrimaryAgreement,
				IsSigned = false,
				ReferenceId = Guid.NewGuid().ToString("N"),
				TimeCreated = DateTime.UtcNow,
				UserId = user.Id,
				Secret = appConfig.Services.SignRequest.Auth,
			};

			// add/save
			dbContext.SignedDocument.Add(request);
			await dbContext.SaveChangesAsync();

			// set new unverified agreement 
			user.UserVerification.SignedAgreementId = null;
			user.UserVerification.LastAgreementId = request.Id;
			dbContext.Update(user.UserVerification);
			await dbContext.SaveChangesAsync();

			return await docService.SendPrimaryAgreementRequest(
				refId: request.ReferenceId,
				name: user.UserVerification.FirstName + " " + user.UserVerification.LastName,
				email: email,
				date: DateTime.UtcNow
			); ;
		}

		// ---

		public sealed class CreateUserAccountResult {

			public User User { get; set; }
			public bool IsEmailExists { get; set; }
			public bool IsUsernameExists { get; set; }
		}

		/// <summary>
		/// Custom user manager class
		/// </summary>
		public class GMUserManager : UserManager<User> {

			public GMUserManager(IUserStore<User> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<User> passwordHasher, IEnumerable<IUserValidator<User>> userValidators, IEnumerable<IPasswordValidator<User>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<User>> logger) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger) {
			}

			public override Task<string> GenerateTwoFactorTokenAsync(User user, string tokenProvider) {
				return Task.FromResult(Tokens.GoogleAuthenticator.Generate(user.TFASecret));
			}

			public override Task<IList<string>> GetValidTwoFactorProvidersAsync(User user) {
				return Task.FromResult(new List<string>() { "GoogleAuthenticator" } as IList<string>);
			}

			public override Task<bool> VerifyTwoFactorTokenAsync(User user, string tokenProvider, string token) {
				return Task.FromResult(Tokens.GoogleAuthenticator.Validate(token, user.TFASecret));
			}
		}
	}
}
