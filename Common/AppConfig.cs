using System;

namespace Goldmint.Common {

	public class AppConfig {

		public ConnectionStringsSection ConnectionStrings { get; set; } = new ConnectionStringsSection();
		public class ConnectionStringsSection {
			public string Default { get; set; } = "";
		}

		// ---

		public AppsSection Apps { get; set; } = new AppsSection();

		public class AppsSection {

			public string RelativeApiPath { get; set; } = "/";

			public CabinetSection Cabinet { get; set; } = new CabinetSection();
			public DashboardSection Dashboard { get; set; } = new DashboardSection();

			// ---

			public class CabinetSection : BaseAppSection {

				public string RouteVerificationPage { get; set; } = "";
				public string RouteSignUpConfirmation { get; set; } = "";
				public string RoutePasswordRestoration { get; set; } = "";
				public string RouteEmailTaken { get; set; } = "";
				public string RouteOAuthTfaPage { get; set; } = "";
				public string RouteOAuthAuthorized { get; set; } = "";
				public string RouteDpaRequired { get; set; } = "";
				public string RouteDpaSigned { get; set; } = "";
			}

			public class DashboardSection : BaseAppSection {
			}

			public abstract class BaseAppSection {

				public string Url { get; set; } = "/";
			}
		}

		// ---

		public AuthSection Auth { get; set; } = new AuthSection();
		public class AuthSection {

			public JwtSection Jwt { get; set; } = new JwtSection();
			public class JwtSection {

				public string Issuer { get; set; } = "";
				public string Secret { get; set; } = "";
				public AudienceSection[] Audiences { get; set; } = new AudienceSection[0];

				public class AudienceSection {
					public string Audience { get; set; } = "";
					public long ExpirationSec { get; set; } = 1800;
				}
			}

			public string TwoFactorIssuer { get; set; } = "goldmint.io";

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

			public ZendeskSsoSection ZendeskSso { get; set; } = new ZendeskSsoSection();
			public class ZendeskSsoSection {
				public string JwtSecret { get; set; } = "";
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
				public string CallbackSecret { get; set; } = "";
			}

			public EthereumSection Ethereum { get; set; } = new EthereumSection();
			public class EthereumSection {

				public string StorageControllerContractAbi { get; set; } = "";
				public string StorageControllerContractAddress { get; set; } = "";
				public string StorageControllerManagerPk { get; set; } = "";

				public string Provider { get; set; } = "";
				public string LogsProvider { get; set; } = "";
				public long MinimalGasLimit { get; set; } = 300000;
				public CryptoExchangeRequestSection CryptoExchangeRequest { get; set; } = new CryptoExchangeRequestSection();
				
				public class CryptoExchangeRequestSection {

					public string FromBlock { get; set; } = "0";
				}
			}

			public IpfsSection Ipfs { get; set; } = new IpfsSection();
			public class IpfsSection {

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
					public string Locale { get; set; }
					public string Filename { get; set; }
					public string Template { get; set; }
				}
			}

			public GMRatesProviderSection GMRatesProvider { get; set; } = new GMRatesProviderSection();
			public class GMRatesProviderSection {

				public string GoldRateUrl { get; set; } = "";
				public string EthRateUrl { get; set; } = "";
			}
		}

		// ---

		public BusSection Bus { get; set; } = new BusSection();
		public class BusSection {

			public WorkerRatesSection WorkerRates { get; set; } = new WorkerRatesSection();
			public class WorkerRatesSection {

				public string PubUrl { get; set; } = "";
				public int PubPeriodSec { get; set; } = 1;
				public GoldSection Gold { get; set; } = new GoldSection();
				public EthSection Eth { get; set; } = new EthSection();

				public class GoldSection {

					public int PeriodSec { get; set; } = 3600;
					public int ValidForSec { get; set; } = 3666;
				}

				public class EthSection {

					public int PeriodSec { get; set; } = 30;
					public int ValidForSec { get; set; } = 60;
				}
			}
		}

		// ---

		public ConstantsSection Constants { get; set; } = new ConstantsSection();
		public class ConstantsSection {

			/*public FiatLimitsSection FiatAccountLimitsUsd { get; set; } = new FiatLimitsSection();
			public class FiatLimitsSection {

				public Limits Tier1 { get; set; } = new Limits();
				public Limits Tier2 { get; set; } = new Limits();

				public class Limits {
					public long DayDeposit { get; set; } = 0;
					public long MonthDeposit { get; set; } = 0;
					public long DayWithdraw { get; set; } = 0;
					public long MonthWithdraw { get; set; } = 0;
				}
			}*/

			/*public CardPaymentDataSection CardPaymentData { get; set; } = new CardPaymentDataSection();
			public class CardPaymentDataSection {

				public long DepositMin { get; set; } = 1;
				public long DepositMax { get; set; } = 0;
				public long WithdrawMin { get; set; } = 1;
				public long WithdrawMax { get; set; } = 0;
			}*/

			/*public SwiftDataSection SwiftData { get; set; } = new SwiftDataSection();
			public class SwiftDataSection {

				public long DepositMin { get; set; } = 1;
				public long DepositMax { get; set; } = 0;
				public long WithdrawMin { get; set; } = 1;
				public long WithdrawMax { get; set; } = 0;

				public string BenName { get; set; } = "";
				public string BenAddress { get; set; } = "";
				public string BenIban { get; set; } = "";
				public string BenBankName { get; set; } = "";
				public string BenBankAddress { get; set; } = "";
				public string BenSwift { get; set; } = "";
			}*/

			/*public CryptoCapitalDataSection CryptoCapitalData { get; set; } = new CryptoCapitalDataSection();
			public class CryptoCapitalDataSection {

				public long DepositMin { get; set; } = 1;
				public long DepositMax { get; set; } = 0;
				public long WithdrawMin { get; set; } = 1;
				public long WithdrawMax { get; set; } = 0;
			}*/

			public SafeExchangeFixedRateThresholdSection SafeExchangeFixedRateThreshold { get; set; } = new SafeExchangeFixedRateThresholdSection();
			public class SafeExchangeFixedRateThresholdSection {

				public Asset Gold { get; set; } = new Asset();
				public Asset Eth { get; set; } = new Asset();

				public class Asset {

					public double Buy { get; set; } = 0.1d;
					public double Sell { get; set; } = 0.1d;
				}
			}

			public TimeLimitsSection TimeLimits { get; set; } = new TimeLimitsSection();
			public class TimeLimitsSection {

				public int BuyGoldForEthRequestTimeoutSec { get; set; } = 1800;
				public int SellGoldForEthRequestTimeoutSec { get; set; } = 1800;
			}

			public WorkersSection Workers { get; set; } = new WorkersSection();
			public class WorkersSection {

				public WorkerSettings Notifications { get; set; } = new WorkerSettings();
				public WorkerSettings EthEventsHarvester { get; set; } = new WorkerSettings();
				public WorkerSettings EthereumOprations { get; set; } = new WorkerSettings();

				public class WorkerSettings {

					public int PeriodSec { get; set; } = 10;
					public int ItemsPerRound { get; set; } = 100;
					public int EthConfirmations { get; set; } = 6;
				}
			}
		}
	}
}
