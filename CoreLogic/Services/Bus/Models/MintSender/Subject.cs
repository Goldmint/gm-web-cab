namespace MintSender.Subject.Sender {

	public static class Request {
		public const string Send = "mint.mintsender.sender.send";
	}
	public static class Event {
		public const string Sent = "mint.mintsender.sender.sent";
	}
}

namespace MintSender.Subject.Watcher {

	public static class Request {
		public const string AddRemove = "mint.mintsender.watcher.watch";
	}
	public static class Event {
		public const string Refill = "mint.mintsender.watcher.refill";
	}
}
