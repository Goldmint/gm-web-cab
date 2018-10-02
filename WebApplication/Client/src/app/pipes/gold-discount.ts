import { Pipe, PipeTransform } from '@angular/core';

@Pipe({name: 'goldDiscount'})
export class GoldDiscount implements PipeTransform {
  transform(value: number, discount: number) {
    if (value) {
      let gold = discount * value / 100;
      let position = gold.toString().indexOf('.');
      let data;

      if (position >= 0) {
        data = (gold.toString().substr(0, position + 9)).replace(/0+$/, '');
        return +data;
      } else {
        data = gold.toString().replace(/0+$/, '');
        return +data;
      }
    }
  }
}