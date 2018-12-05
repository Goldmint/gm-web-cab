export interface PawnshopDetails {
  daily_stats: DailyStats[];
  id: number;
  name: string;
  org_id: number;
  sources: Sources[];
}

interface DailyStats {
  closed_amount: number;
  closed_count: number;
  currently_opened_amount: number;
  opened_amount: number;
  opened_count: number;
  sold_amount: number;
  sold_count: number;
  time: number;
}

interface Sources {
  id: number;
  mnt_balance: string;
  type: string;
  wallet: string;
}