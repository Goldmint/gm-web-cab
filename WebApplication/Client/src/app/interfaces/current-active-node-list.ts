export interface CurrentActiveNodeList {
  activity: number;
  address: string;
  balance_gold: string;
  balance_mnt: string;
  created_at: number;
  gained_gold: string;
  gained_mnt: string;
  history: History[];
  name: string;
  quit_at: any;
  chartData?: any;
}

interface History {
  time: number;
  gold: number;
  mnt: number;
}