export interface PagedData<T> {
  items  : T[];
  offset : number;
  limit  : number;
  total  : number;
}
