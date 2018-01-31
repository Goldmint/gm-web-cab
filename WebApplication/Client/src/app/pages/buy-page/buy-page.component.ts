import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
// import { TranslateService, LangChangeEvent } from '@ngx-translate/core';

@Component({
  selector: 'app-buy-page',
  templateUrl: './buy-page.component.html',
  styleUrls: ['./buy-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'page' }
})
export class BuyPageComponent implements OnInit {

  to_spend: number;
  estimate_amount: number;
  discount: number = 0;

  public buyCurrency:     'usd'|'gold' = 'usd';
  public resultCurrrency: 'usd'|'gold' = 'gold';

  constructor(private _cdRef: ChangeDetectorRef/*, private translate: TranslateService*/) { }

  ngOnInit() {
  }

  onToSpendChanged(value: number) {
    this.to_spend = value;
    this.estimate_amount = value && value > 0 ? value / 2 : 0;

    this.discount = value && value > 0 ? Math.floor(Math.random() * 100) : 0;

    this._cdRef.detectChanges();
  }

  onCurrencyChanged(value: string) {
    this.resultCurrrency = (value === 'usd') ? 'gold' : 'usd';
  }

}
