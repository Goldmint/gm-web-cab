import { Injectable } from '@angular/core';
import {Subject} from "rxjs/Subject";
import {BehaviorSubject} from "rxjs/BehaviorSubject";

@Injectable()
export class CommonService {

  public changeFeedTab = new Subject();
  public getPawnShopOrganization = new BehaviorSubject(null);

  constructor() { }

  public highlightNewItem(currentRows: any[], prevRows: any[], className: string, selector: string) {
    let rows = currentRows.slice();
    let newItemsId = [];

    for (let i = 0; i < currentRows.length; i++) {
      if (prevRows.length && currentRows[i][selector] !== prevRows[0][selector]) {
        newItemsId.push(currentRows[i]);
      } else {
        break;
      }
    }

    newItemsId.forEach((id, index) => {
      setTimeout(() => {
        const elem = document.querySelector('.' + className + '-' + index);
        elem && elem.classList.add('new-table-item')
        setTimeout(() => {
          elem && elem.classList.remove('new-table-item');
        }, 8000);
      }, 500);

    });

    return rows;
  }

}
