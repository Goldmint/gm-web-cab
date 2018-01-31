import { Price } from './price';

export interface TransparencyRecord {
  date   : number;
  amount : Price;
  link   : string;
}
