import { Region } from './region';

export interface Country {
  countryName      : string;
  countryShortCode : string;
  regions          : Region[];
}
