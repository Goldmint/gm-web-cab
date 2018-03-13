using System.Threading.Tasks;
using Goldmint.Common;

namespace Goldmint.CoreLogic.Services.Localization {

	public interface ITemplateProvider {

		Task<EmailTemplate> GetEmailTemplate(string name, Locale locale);
	}
}
