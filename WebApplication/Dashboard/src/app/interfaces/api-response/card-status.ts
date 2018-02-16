export type CardStatus = 'initial'|'confirm'|'payment'|'verification'|'verified'|'disabled'|'failed';

export interface CardStatusResponse {
  status : CardStatus;
}
