import { Injectable } from "@angular/core";
import { Observable } from "rxjs/Observable";
import { interval } from "rxjs/observable/interval";

import { BehaviorSubject } from "rxjs/BehaviorSubject";
import { APIService } from "./api.service";

@Injectable()
export class GoldrateService {

  private _obsRateSubject = new BehaviorSubject<number | null>(null);
  private _obsRate: Observable<number|null> = this._obsRateSubject.asObservable();

  constructor(
    private _apiService: APIService
  ) {

    console.log('GoldrateService constructor');

    this.checkBalance();
    interval(5000)
      .subscribe(time => {
        this.checkBalance();
      })
      ;
  }
  
  private checkBalance() {
    this._apiService.getGoldRate().subscribe(res => {
      this._obsRateSubject.next(res.data.rate);
    });
  }
  
  public getObservableRate(): Observable<number|null> {
    return this._obsRate;
  }
}
