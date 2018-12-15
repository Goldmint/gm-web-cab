import {Balance} from "./balance";

export interface WalletInfo {
  approved_nonce: string;
  balance: Balance;
  exists: boolean;
  tags: string[];
}