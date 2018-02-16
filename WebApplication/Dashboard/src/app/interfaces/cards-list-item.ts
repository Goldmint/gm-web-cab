import { CardStatus } from "./api-response/card-status";

export interface CardsListItem {
  cardId : number;
  mask   : string;
  status : CardStatus;
}
