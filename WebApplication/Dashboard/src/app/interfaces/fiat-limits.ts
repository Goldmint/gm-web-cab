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
