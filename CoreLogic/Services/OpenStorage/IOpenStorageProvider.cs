using System.IO;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.OpenStorage {

	public interface IOpenStorageProvider {

		/// <summary>
		/// Upload file and get link
		/// </summary>
		Task<string> UploadFile(Stream fileStream, string filename);

	}
}