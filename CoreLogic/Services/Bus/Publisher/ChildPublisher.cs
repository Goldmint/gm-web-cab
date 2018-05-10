using NLog;
using System;

namespace Goldmint.CoreLogic.Services.Bus.Publisher {

	public sealed class ChildPublisher : DefaultPublisher {

		public ChildPublisher(Uri bindUri, LogFactory logFactory) : base(bindUri, logFactory) { }
	}
}
