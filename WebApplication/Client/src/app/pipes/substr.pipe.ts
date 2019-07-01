import { Pipe, PipeTransform } from '@angular/core';

@Pipe({name: 'substr'})
export class SubstrPipe implements PipeTransform {
  transform(value: number, digits: number, useFormatting: boolean) {
    const position = value.toString().indexOf('.');
    let result;
    if (position >= 0) {
      result = value.toString().substr(0, position + digits + 1);
    } else {
      result = value.toString();
    }

    if (useFormatting !== undefined) {
      result = result.replace(/\d(?=(\d{3})+\.)/g, '$& ');
    }

    return result;
  }
}
