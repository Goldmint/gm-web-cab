import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';

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

  constructor(private _cdRef: ChangeDetectorRef) { }

  ngOnInit() {
  }

  chooseCurrencyType(type) {
    if (this.currentCurrencyType !== type) {
      this.currentCurrencyType = type;
      this._cdRef.detectChanges();
    }
  }
}
