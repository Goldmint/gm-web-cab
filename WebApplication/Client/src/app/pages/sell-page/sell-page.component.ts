import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding } from '@angular/core';

@Component({
  selector: 'app-sell-page',
  templateUrl: './sell-page.component.html',
  styleUrls: ['./sell-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SellPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  to_sell: number;
  estimate_amount: number;
  discount: number = 0;

  public sellCurrency:    'usd'|'gold' = 'gold';
  public resultCurrrency: 'usd'|'gold' = 'usd';

  constructor(private _cdRef: ChangeDetectorRef) { }

  ngOnInit() {
  }

  onToSellChanged(value: number) {
    this.to_sell = value;
    this.estimate_amount = value * 2;

    this.discount = value && value > 0 ? Math.floor(Math.random() * 100) : 0;

    this._cdRef.detectChanges();
  }

  onCurrencyChanged(value: string) {
    this.resultCurrrency = (value === 'usd') ? 'gold' : 'usd';
  }

}
