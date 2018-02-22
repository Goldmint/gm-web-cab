using System;

namespace Goldmint.Common {

	public class AppConfig {

		public ConnectionStringsSection ConnectionStrings { get; set; } = new ConnectionStringsSection();
		public class ConnectionStringsSection {
			public string Default { get; set; } = "";
		}

		// ---

		public AppRoutesSection AppRoutes { get; set; } = new AppRoutesSection();
		public class AppRoutesSection {

			public string SignUpConfirmation { get; set; } = "";
			public string PasswordRestoration { get; set; } = "";
			public string EmailTaken { get; set; } = "";
			public string OAuthTFAPage { get; set; } = "";
			public string OAuthAuthorized { get; set; } = "";
		}

		// ---

		public AuthSection Auth { get; set; } = new AuthSection();
		public class AuthSection {

			public JWTSection JWT { get; set; } = new JWTSection();
			public class JWTSection {

				public string Secret { get; set; } = "";
				public AudienceSection[] Audiences { get; set; } = new AudienceSection[0];

				public class AudienceSection {
					public string Audience { get; set; } = "";
					public long ExpirationSec { get; set; } = 1800;
				}
			}

			public FacebookSection Facebook { get; set; } = new FacebookSection();
			public class FacebookSection {
				public string AppId { get; set; } = "";
				public string AppSecret { get; set; } = "";
			}

			public GoogleSection Google { get; set; } = new GoogleSection();
			public class GoogleSection {
				public string ClientId { get; set; } = "";
				public string ClientSecret { get; set; } = "";
			}
		}

		// ---

		public ServicesSection Services { get; set; } = new ServicesSection();
		public class ServicesSection {

			public RecaptchaSection Recaptcha { get; set; } = new RecaptchaSection();
			public class RecaptchaSection {

				public string SiteKey { get; set; } = "";
				public string SecretKey { get; set; } = "";
			}

			public MailGunSection MailGun { get; set; } = new MailGunSection();
			public class MailGunSection {

				public string Url { get; set; } = "";
				public string DomainName { get; set; } = "";
				public string Key { get; set; } = "";
				public string Sender { get; set; } = "";
			}

			public ShuftiProSection ShuftiPro { get; set; } = new ShuftiProSection();
			public class ShuftiProSection {

				public string ClientId { get; set; } = "";
				public string ClientSecret { get; set; } = "";
			}

			public The1stPaymentsSection The1stPayments { get; set; } = new The1stPaymentsSection();
			public class The1stPaymentsSection {

				public string MerchantGuid { get; set; } = "";
				public string ProcessingPassword { get; set; } = "";
				public string Gateway { get; set; } = "";
			}

			public InfuraSection Infura { get; set; } = new InfuraSection();
			public class InfuraSection {

				public string EthereumNetUrl { get; set; } = "";
				public long DefaultGasPriceWei { get; set; } = 0;
				public string GMAccountPrivateKey { get; set; } = "";
				public string FiatContractAddress { get; set; } = "";
				public string FiatContractABI { get; set; } = "";
			}

			public IPFSSection IPFS { get; set; } = new IPFSSection();
			public class IPFSSection {

				public string Url { get; set; } = "";
			}

			public SignRequestSection SignRequest { get; set; } = new SignRequestSection();
			public class SignRequestSection {

				public string Url { get; set; } = "";
				public string Auth { get; set; } = "";
				public string SenderEmail { get; set; } = "";
				public string CallbackSecret { get; set; } = "";
				public TemplateSection[] Templates { get; set; } = new TemplateSection[0];

				public class TemplateSection {

					public string Name { get; set; }
					public string Filename { get; set; }
					public string Template { get; set; }
				}
			}
		}

		// ---

		public RpcServicesSection RpcServices { get; set; } = new RpcServicesSection();
		public class RpcServicesSection {

			public string GoldRateUsdUrl { get; set; } = "";

		}

		// ---

		public ConstantsSection Constants { get; set; } = new ConstantsSection();
		public class ConstantsSection {

			public double ExchangeThreshold { get; set; } = 0.5d;

			public FiatLimitsSection FiatAccountLimitsUSD { get; set; } = new FiatLimitsSection();
			public class FiatLimitsSection {

				public Limits L0 { get; set; } = new Limits();
				public Limits L1 { get; set; } = new Limits();

				public class Limits {
					public long DayDeposit { get; set; } = 0;
					public long MonthDeposit { get; set; } = 0;
					public long DayWithdraw { get; set; } = 0;
					public long MonthWithdraw { get; set; } = 0;
				}
			}

			public CardPaymentDataSection CardPaymentData { get; set; } = new CardPaymentDataSection();
			public class CardPaymentDataSection {

				public long DepositMin { get; set; } = 0;
				public long DepositMax { get; set; } = 0;
				public long WithdrawMin { get; set; } = 0;
				public long WithdrawMax { get; set; } = 0;
			}

			public SwiftDataSection SwiftData { get; set; } = new SwiftDataSection();
			public class SwiftDataSection {

				public long DepositMin { get; set; } = 0;
				public long DepositMax { get; set; } = 0;
				public long WithdrawMin { get; set; } = 0;
				public long WithdrawMax { get; set; } = 0;

				public string BenName { get; set; } = "";
				public string BenAddress { get; set; } = "";
				public string BenIban { get; set; } = "";
				public string BenBankName { get; set; } = "";
				public string BenBankAddress { get; set; } = "";
				public string BenSwift { get; set; } = "";
			}
		}
	}
}
