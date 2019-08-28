import { Injectable } from '@angular/core';
import {Subject} from "rxjs/Subject";
import {BehaviorSubject} from "rxjs/BehaviorSubject";

@Injectable()
export class CommonService {

  public changeFeedTab = new Subject();
  public getPawnShopOrganization = new BehaviorSubject(null);
  public getActiveMenuItem = new BehaviorSubject(null);
  public buyAmount: {euro: number, gold: number, isGold: boolean} | any = null;

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

  public substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d{1,6})\d*(?:(\.\d{0,6})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  public setCookie(name, value, options = {}) {
    let data: any = {
      path: '/',
      ...options
    };

    if (data.expires && data.expires.toUTCString) {
      data.expires = data.expires.toUTCString();
    }

    let updatedCookie = encodeURIComponent(name) + "=" + encodeURIComponent(value);

    for (let key in data) {
      updatedCookie += "; " + key;
      let optionValue = data[key];
      if (optionValue !== true) {
        updatedCookie += "=" + optionValue;
      }
    }
    document.cookie = updatedCookie;
  }

  public getCookie(name) {
    let matches = document.cookie.match(new RegExp(
      "(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"
    ));
    return matches ? decodeURIComponent(matches[1]) : undefined;
  }

  public deleteFxCookies() {
    this.setCookie('fx_deposit_amount', '', {'max-age': -1});
    this.setCookie('fx_url', '', {'max-age': -1});
    this.setCookie('fx_buy_data', '', {'max-age': -1});
  }

}
