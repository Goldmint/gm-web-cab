export interface TransactionInfo {
  status: string;
  transaction: {
    amount_gold: string;
    amount_mnt: string;
    block: string;
    data_piece: string;
    data_size: number;
    digest: string;
    from: string;
    name: string;
    nonce: number;
    timestamp: number;
    to: string;
  }
}