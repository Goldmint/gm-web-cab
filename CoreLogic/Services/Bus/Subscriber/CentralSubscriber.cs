using Goldmint.CoreLogic.Services.Bus.Proto;
using NLog;
using System;

namespace Goldmint.CoreLogic.Services.Bus.Subscriber {

	public sealed class CentralSubscriber : DefaultSubscriber {

		public CentralSubscriber(Topic[] topics, Uri connectUri, LogFactory logFactory) : base(topics, connectUri, logFactory) { }
	}
}
