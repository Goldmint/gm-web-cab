using System;
using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.RuntimeConfig {

	public interface IRuntimeConfigUpdater {

		Task PublishUpdated();
	}
}
