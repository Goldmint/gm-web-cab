namespace Goldmint.Common {

	public sealed class AppConfig {

		public ConnectionStringsSection ConnectionStrings { get; set; } = new ConnectionStringsSection();
		public class ConnectionStringsSection {
			public string Default { get; set; } = "";
        }

		// ---

		public AppsSection Apps { get; set; } = new AppsSection();
		public class AppsSection {

			public string RelativeApiPath { get; set; } = "/";

			public CabinetSection Cabinet { get; set; } = new CabinetSection();

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

			public abstract class BaseAppSection {

				public string[] Url { get; set; } = new string[]{};
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
				public string CallbackSecret { get; set; } = "";
			}

			public EthereumSection Ethereum { get; set; } = new EthereumSection();
			public class EthereumSection {

				public string MntpContractAbi { get; set; } = "";
				public string MntpContractAddress { get; set; } = "";

				public string PoolContractAbi { get; set; } = "";
				public string PoolContractAddress { get; set; } = "";

				public string PoolFreezerContractAbi { get; set; } = "";
				public string PoolFreezerContractAddress { get; set; } = "";

				public string EtherscanTxView { get; set; } = "";
				public string Provider { get; set; } = "";

				public int ConfirmationsRequired {get; set;} = 12;
				public string EthSenderPk { get; set; } = "";
			}

			//public SignRequestSection SignRequest { get; set; } = new SignRequestSection();
			//public class SignRequestSection {

			//	public string Url { get; set; } = "";
			//	public string Auth { get; set; } = "";
			//	public string SenderEmail { get; set; } = "";
			//	public string CallbackSecret { get; set; } = "";
			//	public TemplateSection[] Templates { get; set; } = new TemplateSection[0];

			//	public class TemplateSection {

			//		public string Name { get; set; }
			//		public string Locale { get; set; }
			//		public string Filename { get; set; }
			//		public string Template { get; set; }
			//	}
			//}

			public GMRatesProviderSection GMRatesProvider { get; set; } = new GMRatesProviderSection();
			public class GMRatesProviderSection {

				public int RequestTimeoutSec { get; set; } = 30;
				public string GoldRateUrl { get; set; } = "";
				public string EthRateUrl { get; set; } = "";
			}
		}

		// ---

		public BusSection Bus { get; set; } = new BusSection();
		public class BusSection {

			public NatsSection Nats { get; set; } = new NatsSection();
			public class NatsSection {

				public string Endpoint { get; set; } = "localhost:4222";
			}
		}
	}
}
