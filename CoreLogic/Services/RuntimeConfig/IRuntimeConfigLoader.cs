using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.RuntimeConfig {

	public interface IRuntimeConfigLoader {

		Task<string> Load();
		Task<bool> Save(string json);
	}
}
