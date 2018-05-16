using NLog;
using System;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public sealed class ChildPublisher : DefaultPublisher {

		public ChildPublisher(int port, LogFactory logFactory) : base(port, logFactory) { }
	}
}
