export interface TransactionInfo {
  status: number,
  time: string,
  tx: {
    blockNumber: number,
    createDate: string,
    destinationWallet: string,
    id: number,
    sourceWallet: string,
    timeStamp: any,
    tokenType: string,
    tokensCount: number,
    transactionFee: number,
    transactionId: number,
    uniqueId: string
  }
}