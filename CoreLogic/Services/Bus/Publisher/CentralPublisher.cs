using NLog;
using System;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public sealed class CentralPublisher : DefaultPublisher {

		public CentralPublisher(Uri bindUri, LogFactory logFactory) : base(bindUri, logFactory) { }
	}
}
