using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Localization {

	public interface ITemplateProvider {

		Task<EmailTemplate> GetEmailTemplate(string name, Locale locale = null);
		Task<SwiftTemplate> GetSwiftTemplate(string name, Locale locale = null);
	}
}
