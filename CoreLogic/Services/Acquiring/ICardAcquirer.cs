using System.Threading.Tasks;

namespace Goldmint.CoreLogic.Services.Acquiring {

	public interface ICardAcquirer {

		Task<StartCardStoreResult> StartPaymentCardStore(StartPaymentCardStore data);
		Task<StartCardStoreResult> StartCreditCardStore(StartCreditCardStore data);
		Task<StartCardStoreResult> StartP2PCardStore(StartP2PCardStore data);
		Task<CheckStoreCardResult> CheckCardStored(string gwStoreTransactionId);

		Task<string> StartCreditCharge(StartCreditCharge data);
		Task<string> StartP2PCharge(StartP2PCharge data);
		Task<string> StartPaymentCharge(StartPaymentCharge data);

		Task<ChargeResult> DoCreditCharge(string gwTransactionId);
		Task<ChargeResult> DoP2PCharge(string gwTransactionId);
		Task<ChargeResult> DoPaymentCharge(string gwTransactionId);

		Task<string> RefundPayment(RefundPayment data);

	}
}
