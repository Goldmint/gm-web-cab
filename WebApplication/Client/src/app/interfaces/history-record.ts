import { Price } from './price';

enum OperationType {Withdraw, Deposit}

export interface HistoryRecord {
  date    : number;
  type    : OperationType|'withdraw'|'deposit';
  amount  : Price;
  balance : Price;
  fee     : Price;
  comment : string;
}
