import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';
import {APIService} from "../../../services";

@Component({
  selector: 'app-settings-fees-page',
  templateUrl: './settings-fees-page.component.html',
  styleUrls: ['./settings-fees-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsFeesPageComponent implements OnInit {

  public currencyTypeList = ['fiat', 'crypto']
  public currentCurrencyType = this.currencyTypeList[0];
  public isDataLoaded = false;
  public fees: object;

  constructor(private _cdRef: ChangeDetectorRef,
              private _apiService: APIService,) { }

  ngOnInit() {
    this._apiService.getFees().subscribe(data => {
      this.fees = data.data;
      this.isDataLoaded = true;
      this._cdRef.detectChanges();
    })
  }

  chooseCurrencyType(type) {
    if (this.currentCurrencyType !== type) {
      this.currentCurrencyType = type;
      this._cdRef.detectChanges();
    }
  }
}
