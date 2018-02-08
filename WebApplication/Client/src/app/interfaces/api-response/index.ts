import { PagedData } from './paged-data';

export interface APIResponse<T> {
  data       : T|any;
  count     ?: number; //@todo: remove after switching to GM API (history, transparency)
  errorCode ?: number;
  errorDesc ?: string;
}

export interface APIPagedResponse<T> {
  data       : PagedData<T>|any;
  errorCode ?: number;
  errorDesc ?: string;
}

export * from './auth';
export * from './registration';
export * from './oauth';
export * from './card-add';
export * from './card-status';
export * from './card-confirm';
export * from './paged-data';
export * from './gold-buy';
export * from './gold-buy-dry';
export * from './gold-sell';
export * from './gold-sell-dry';
export * from './user-balance';