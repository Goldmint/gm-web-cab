export interface APIResponse<T> {
  data       : T|any;
  count     ?: number; //@todo: remove after switching to GM API (history, transparency)
  errorCode ?: number;
  errorDesc ?: string;
}

export * from './auth';
export * from './oauth';
