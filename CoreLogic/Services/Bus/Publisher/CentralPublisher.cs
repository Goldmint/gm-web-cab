using NLog;
using System;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public sealed class CentralPublisher : DefaultPublisher {

		public CentralPublisher(int port, LogFactory logFactory) : base(port, logFactory) { }
	}
}
