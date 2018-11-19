import { Injectable } from '@angular/core';
import {BehaviorSubject} from "rxjs/BehaviorSubject";

@Injectable()
export class CommonService {

  public organizationStepper$ = new BehaviorSubject(null);
  public setTwoOrganizationStep$ = new BehaviorSubject(null);

  constructor() { }

  public highlightNewItem(currentRows, prevRows) {
    let rows = currentRows.slice();
    let newItemsId = [];

    for (let i = 0; i < currentRows.length; i++) {
      if (prevRows.length && currentRows[i].id !== prevRows[0].id) {
        newItemsId.push(currentRows[i].id);
      } else {
        break;
      }
    }

    newItemsId.forEach(id => {
      setTimeout(() => {
        const elem = document.querySelector('.table-row-'+ id);
        elem && elem.classList.add('new-table-item')
        setTimeout(() => {
          elem && elem.classList.remove('new-table-item');
        }, 8000);
      }, 500);

    });

    return rows;
  }

}
