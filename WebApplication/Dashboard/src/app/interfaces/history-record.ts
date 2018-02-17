import { Price } from './price';

enum OperationType {Withdraw, Deposit, GoldBuying, GoldSelling}

export interface HistoryRecord {
  date    : number;
  type    : OperationType|'withdraw'|'deposit'|'goldbuying'|'goldselling';
  amount  : Price;
  fee     : Price;
  comment : string;
}
