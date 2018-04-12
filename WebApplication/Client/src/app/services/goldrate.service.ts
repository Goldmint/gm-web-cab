import { Injectable } from "@angular/core";
import { Observable } from "rxjs/Observable";
import { BehaviorSubject } from "rxjs/BehaviorSubject";
import "../../assets/functions/trading-view.js"

@Injectable()
export class GoldrateService {

  private _obsRateSubject = new BehaviorSubject<number | null>(null);
  private _obsRate: Observable<number|null> = this._obsRateSubject.asObservable();

  constructor() {
    new window['TradingView'].QuotesProvider({
      container_id: 'hif',
      symbols: [
        {'symbol' : 'FX_IDC:XAUUSD', success: (data) => {
          let value = data.last_price.toString();
          let point = value.indexOf('.');
          if (point < 0) {
            value += '.00';
          } else {
            value = value.substr(0, point + 3);
            value.length === point + 2 && (value += '0');
          }

          this._obsRateSubject.next(value);
        }, error : function() {}}
      ]
    });
    console.log('GoldrateService constructor');
  }
  
  public getObservableRate(): Observable<number|null> {
    return this._obsRate;
  }
}
