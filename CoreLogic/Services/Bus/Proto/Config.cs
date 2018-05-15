using ProtoBuf;

namespace Goldmint.CoreLogic.Services.Bus.Proto.Config {

	/// <summary>
	/// Config updated
	/// </summary>
	[ProtoContract]
	public sealed class ConfigUpdatedMessage {

		[ProtoMember(1)]
		public string Username { get; set; }
	}

}
