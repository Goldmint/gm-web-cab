
namespace Goldmint.DAL.Models {

    public static class FieldMaxLength {

	    public const int BlockchainCurrencyAmount = 64;
	    public const int BlockchainAddress = 128;
		public const int EthereumTransactionHash = 66;
	    public const int TransparencyTransactionHash = 128;

		public const int Guid = 32;
		public const int Ip = 32;
		public const int ConcurrencyStamp = 64;
	    public const int Comment = 512;
	    public const int UserAgent = 128;

	    public const int The1StPaymentTxId = 64;
	    public const int The1StPaymentStatus = 64;
	    public const int CreditCardMask = 64;
	    public const int CreditCardHolderName = 128;

        public const int EthereumAddress = 42;
        public const int SumusAddress = 128;
        public const int SumusTransactionHash = 128;

        public const int CustodyBotName = 256;
        public const int CustodyBotSalt = 64;
        public const int CustodyBotId = 64;
        public const int CustodyPaymentRoute = 64;
        public const int CustodyTicket = 32;
        public const int CustodyFileName = 128;
        public const int CustodyFileExtension = 8;
    }
}
