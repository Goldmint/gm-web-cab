import { Price } from './price';

export interface TransparencySummary {
  issued      : Price;
  burnt       : Price;
  circulation : Price;
}
