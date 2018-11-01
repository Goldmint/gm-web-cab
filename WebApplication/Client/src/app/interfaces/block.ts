export interface Block {
  fee_gold: string;
  fee_mnt: string;
  id: string;
  merkle_root: string;
  orchestrator: string;
  prev_digest: string;
  signers: number;
  timestamp: number;
  total_gold: string;
  total_mnt: string;
  total_user_data: number;
  transactions: number;
}