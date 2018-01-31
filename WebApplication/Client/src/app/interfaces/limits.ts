import { Limit } from './limit';

export interface Limits {
  current : {
    deposit  : Limit;
    withdraw : Limit;
  };
  levels  : {
    l0 : {
      deposit  : Limit;
      withdraw : Limit;
    };
    l1 : {
      deposit  : Limit;
      withdraw : Limit;
    };
    // l2 : {
    //   deposit  : Limit;
    //   withdraw : Limit;
    // };
    // l3 : {
    //   deposit  : Limit;
    //   withdraw : Limit;
    // };
  };
}
