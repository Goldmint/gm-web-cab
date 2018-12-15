import { Injectable } from "@angular/core";
import { Observable } from "rxjs/Observable";
import { BehaviorSubject } from "rxjs/BehaviorSubject";
import {APIService} from "./api.service";
import {interval} from "rxjs/observable/interval";

@Injectable()
export class GoldrateService {

  private _obsRateSubject = new BehaviorSubject<{gold: number, eth: number} | null>(null);
  private _obsRate: Observable<{gold: number, eth: number} | null> = this._obsRateSubject.asObservable();

  constructor(private _apiService: APIService) {

    this.checkBalance();
    interval(5000)
      .subscribe(() => {
        this.checkBalance();
      });

  }

  private checkBalance() {
    this._apiService.getGoldRate()
      .subscribe(res => {
        let rate = {
          gold: res['result'].usd,
          eth: res['result'].eth / Math.pow(10, 18)
        }
        this._obsRateSubject.next(rate);
      });
  }

  public getObservableRate(): Observable<{gold: number, eth: number} | null> {
    return this._obsRate;
  }
}
