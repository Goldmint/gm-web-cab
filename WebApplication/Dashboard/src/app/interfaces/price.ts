export interface Price {
  amount    : number;
  prefix   ?: string;
  suffix   ?: string;
  /* or */
  // currency  : {
  //   code    : string; // ex.: 'USD'
  //   symbol  : string; // ex.: '$'
  // }
}
