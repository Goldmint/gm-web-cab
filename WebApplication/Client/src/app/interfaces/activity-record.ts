enum ActivityType {Auth, Password, Settings, Deposit, Withdraw, Exchange}

export interface ActivityRecord {
  type    : ActivityType|'auth'|'password'|'settings'|'deposit'|'withdraw'|'exchange';
  ip      : string;
  agent  ?: string;
  date    : number;
  comment : string;
}
