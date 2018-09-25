namespace Goldmint.Common {

	public sealed class AppConfig {

		public ConnectionStringsSection ConnectionStrings { get; set; } = new ConnectionStringsSection();
		public class ConnectionStringsSection {
			public string Default { get; set; } = "";
		    public string CustodyBot { get; set; } = "";
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

			public GoogleSheetsSection GoogleSheets { get; set; } = null;
			public class GoogleSheetsSection {

				public string ClientSecret64 { get; set; } = "";
				public string SheetId { get; set; } = "";
			}

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

			public The1StPaymentsSection The1StPayments { get; set; } = new The1StPaymentsSection();
			public class The1StPaymentsSection {

				public string MerchantGuid { get; set; } = "";
				public string ProcessingPassword { get; set; } = "";
				public string Gateway { get; set; } = "";
				public string RsInitStoreSms3D { get; set; } = "";
				public string RsInitRecurrent3D { get; set; } = "";
				public string RsInitStoreSms { get; set; } = "";
				public string RsInitRecurrent { get; set; } = "";
				public string RsInitStoreCrd { get; set; } = "";
				public string RsInitRecurrentCrd { get; set; } = "";
				public string RsInitStoreP2P { get; set; } = "";
				public string RsInitRecurrentP2P { get; set; } = "";
			}

			public EthereumSection Ethereum { get; set; } = new EthereumSection();
			public class EthereumSection {

				public string StorageControllerContractAbi { get; set; } = "";
				public string StorageControllerContractAddress { get; set; } = "";
				public string StorageControllerManagerPk { get; set; } = "";

				public string EtherscanTxView { get; set; } = "";
				public string Provider { get; set; } = "";
				public string LogsProvider { get; set; } = "";
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

				public int RequestTimeoutSec { get; set; } = 30;
				public string GoldRateUrl { get; set; } = "";
				public string EthRateUrl { get; set; } = "";
			}

			public WorkersSection Workers { get; set; } = new WorkersSection();
			public class WorkersSection {

				public DbWorkerSettings Notifications { get; set; } = new DbWorkerSettings();
				public EthWorkerSettings EthEventsHarvester { get; set; } = new EthWorkerSettings();
				public EthWorkerSettings EthereumOperations { get; set; } = new EthWorkerSettings();
				public WorkerSettings GoldRateUpdater { get; set; } = new WorkerSettings();
				public WorkerSettings CryptoRateUpdater { get; set; } = new WorkerSettings();
				public WorkerSettings TelemetryAggregator { get; set; } = new WorkerSettings();
				public DbWorkerSettings CcPaymentProcessor { get; set; } = new DbWorkerSettings();

				public class WorkerSettings {
					public int PeriodSec { get; set; } = 60;
				}

				public class DbWorkerSettings : WorkerSettings {
					public int ItemsPerRound { get; set; } = 50;
				}

				public class EthWorkerSettings : DbWorkerSettings {
					public int EthConfirmations { get; set; } = 30;
				}
			}
		}

		// ---

		public BusSection Bus { get; set; } = new BusSection();
		public class BusSection {

			public CentralPubSection CentralPub { get; set; } = new CentralPubSection();
			public class CentralPubSection {

				public string Endpoint { get; set; } = "";
				public int PubPort { get; set; } = 6666;
				public RatesSection Rates { get; set; } = new RatesSection();
				public ChildPubEndpointSection[] ChildPubEndpoints { get; set; } = { };

				public class RatesSection {

					public double PubPeriodSec { get; set; } = 1;
					public int GoldValidForSec { get; set; } = 1800;
					public int CryptoValidForSec { get; set; } = 180;
				}

				public class ChildPubEndpointSection {

					public string Name { get; set; } = "";
					public string Endpoint { get; set; } = "";
				}
			}

			public ChildPubSection ChildPub { get; set; } = new ChildPubSection();
			public class ChildPubSection {

				public int PubPort { get; set; } = 6666;
				public double PubTelemetryPeriodSec { get; set; } = 5;
			}
		}
	}
}
