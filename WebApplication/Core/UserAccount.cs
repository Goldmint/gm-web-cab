using Goldmint.Common;
using Goldmint.CoreLogic.Services.Google.Impl;
using Goldmint.CoreLogic.Services.SignedDoc;
using Goldmint.DAL;
using Goldmint.DAL.Models;
using Goldmint.DAL.Models.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Goldmint.Common.Extensions;

namespace Goldmint.WebApplication.Core {

	public static class UserAccount {

		/// <summary>
		/// New user account
		/// </summary>
		public static async Task<CreateUserAccountResult> CreateUserAccount(IServiceProvider services, string email, string password = null, bool emailConfirmed = false) {

			var logger = services.GetLoggerFor(typeof(UserAccount));
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var userManager = services.GetRequiredService<UserManager<User>>();
			var googleSheets = services.GetService<Sheets>();

			var ret = new CreateUserAccountResult() {
			};

			if (string.IsNullOrWhiteSpace(email)) {
				ret.IsEmailExists = true;
				logger.Info("Failed to create user account: invalid email");
				return ret;
			}

			var tfaSecret = GenerateTfaSecret();

			try {
				var sumusWallet = new Common.Sumus.Signer();

				var newUser = new User() {
					UserName = email,
					Email = email,
					TfaSecret = tfaSecret,
					JwtSaltCabinet = GenerateJwtSalt(),
					JwtSaltDashboard = GenerateJwtSalt(),
					EmailConfirmed = emailConfirmed,
					AccessRights = 0,

					UserOptions = new DAL.Models.UserOptions() {
					},
					UserSumusWallet = new DAL.Models.UserSumusWallet {
						PublicKey = sumusWallet.PublicKey,
						PrivateKey = sumusWallet.PrivateKey,
						TimeCreated = DateTime.UtcNow,
						TimeChecked = DateTime.UtcNow,
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

					logger.Info($"User account created {newUser.Id}");

					try {
						var name = string.Format("u{0:000000}", newUser.Id);

						newUser.UserName = name;
						newUser.NormalizedUserName = name.ToUpperInvariant();
						newUser.JwtSaltCabinet = GenerateJwtSalt();
						newUser.JwtSaltDashboard = GenerateJwtSalt();
						newUser.AccessRights = 1;

						await dbContext.SaveChangesAsync();

						logger.Info($"User account {newUser.Id} prepared and saved");

						if (googleSheets != null) {
							try {
								await googleSheets.InsertUser(
									new UserInfoCreate() {
										UserId = newUser.Id,
										UserName = newUser.UserName,
										FirstName = "-",
										LastName = "-",
										Country = "-",
										Birthday = "-",
									}
								);
							}
							catch (Exception e) {
								logger.Error(e, "Failed to persist user account creation in Google Sheets");
							}
						}
					}
					catch { }
				}
				else {
					foreach (var v in result.Errors) {
						if (v.Code == "DuplicateUserName") {
							ret.IsUsernameExists = true;
							logger.Info($"Failed to create user account: duplicate username");
						}
						else if (v.Code == "DuplicateEmail") {
							ret.IsEmailExists = true;
							logger.Info($"Failed to create user account: duplicate email");
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
		private static string GenerateJwtSalt() {
			return SecureRandom.GetString09azAZ(64);
		}

		/// <summary>
		/// Randomize access stamp
		/// </summary>
		public static void GenerateJwtSalt(User user, JwtAudience audience) {
			switch (audience) {
				case JwtAudience.Cabinet: user.JwtSaltCabinet = GenerateJwtSalt(); break;
				default: throw new NotImplementedException("Audience is not implemented");
			}
		}
		
		/// <summary>
		/// Current access stamp
		/// </summary>
		public static string CurrentJwtSalt(User user, JwtAudience audience) {
			if (user == null) return null;
			switch (audience) {
				case JwtAudience.Cabinet: return user.JwtSaltCabinet;
				default: throw new NotImplementedException("Audience is not implemented");
			}
		}

		/// <summary>
		/// Random TFA secret
		/// </summary>
		public static string GenerateTfaSecret() {
			return SecureRandom.GetString09azAZSpecs(14);
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
		public class GmUserManager : UserManager<User> {

			public GmUserManager(IUserStore<User> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<User> passwordHasher, IEnumerable<IUserValidator<User>> userValidators, IEnumerable<IPasswordValidator<User>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<User>> logger) : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger) {
			}

			public override Task<string> GenerateTwoFactorTokenAsync(User user, string tokenProvider) {
				return Task.FromResult(Tokens.GoogleAuthenticator.Generate(user.TfaSecret));
			}

			public override Task<IList<string>> GetValidTwoFactorProvidersAsync(User user) {
				return Task.FromResult(new List<string>() { "GoogleAuthenticator" } as IList<string>);
			}

			public override Task<bool> VerifyTwoFactorTokenAsync(User user, string tokenProvider, string token) {
				return Task.FromResult(Tokens.GoogleAuthenticator.Validate(token, user.TfaSecret));
			}
		}
	}
}
