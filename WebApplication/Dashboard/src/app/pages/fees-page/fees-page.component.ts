import {ChangeDetectorRef, Component, OnInit} from '@angular/core';

@Component({
  selector: 'app-fees-page',
  templateUrl: './fees-page.component.html',
  styleUrls: ['./fees-page.component.sass']
})
export class FeesPageComponent implements OnInit {

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

  updateFees() {

  }

}
