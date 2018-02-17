export interface FiatLimits {
  current: {
    deposit: FiatLimitUserOperation;
    withdraw: FiatLimitUserOperation;
  };
  levels: {
    current: FiatLimitLevel;
    l0: FiatLimitLevel;
    l1: FiatLimitLevel;
  };
  paymentMethod?: {
    card: {
      deposit: paymentLimits;
      withdraw: paymentLimits;
    };
    swift: {
      deposit: paymentLimits;
      withdraw: paymentLimits;
    }
  };
}

export interface FiatLimitLevel {
  deposit: FiatLimitOperation;
  withdraw: FiatLimitOperation;
}

export interface FiatLimitOperation {
  day: number;
  month: number;
}

export interface FiatLimitUserOperation {
  minimal: number;
  day: number;
  month: number;
}

export interface paymentLimits {
  min: number;
  max: number;
}
