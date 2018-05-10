using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Goldmint.CoreLogic.Services.Bus.Proto {

	[ProtoContract]
	public sealed class ApiServerStatusMessage {

		[ProtoMember(1)]
		public string Name { get; set; }
	}

	[ProtoContract]
	public sealed class CoreServerStatusMessage {

		[ProtoMember(1)]
		public string Name { get; set; }
	}

	[ProtoContract]
	public sealed class WorkerServerStatusMessage {

		[ProtoMember(1)]
		public string Name { get; set; }
	}

	[ProtoContract]
	public sealed class SystemOverallStatusMessage {

		[ProtoMember(1)]
		public WorkerServerStatusMessage WorkerServer { get; set; }

		[ProtoMember(2)]
		public ApiServerStatusMessage[] ApiServers { get; set; }

		[ProtoMember(3)]
		public CoreServerStatusMessage[] CoreServers { get; set; }
	}
}
