import { Pipe, PipeTransform } from '@angular/core';

@Pipe({name: 'reduction'})
export class AccountReductionPipe implements PipeTransform {
  transform(value: string, firstCount: number, lastCount: number) {
    if (value) return value.slice(0, firstCount) + '....' + value.slice(-lastCount);
  }
}