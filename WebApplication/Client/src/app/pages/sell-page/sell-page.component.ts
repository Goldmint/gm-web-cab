import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding } from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService } from '../../services';

@Component({
  selector: 'app-sell-page',
  templateUrl: './sell-page.component.html',
  styleUrls: ['./sell-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SellPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  progress: boolean;
  to_sell: number;
  estimate_amount: number;
  discount: number = 0;

  public sellCurrency:    'usd'|'gold' = 'gold';
  public resultCurrrency: 'usd'|'gold' = 'usd';

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
  }

  onToSellChanged(value: number) {
    this.to_sell = value;

    this.estimate_amount = 0; // value * 2;
    // this.discount = value && value > 0 ? Math.floor(Math.random() * 100) : 0;

    this._cdRef.detectChanges();
  }

  onCurrencyChanged(value: string) {
    this.resultCurrrency = (value === 'usd') ? 'gold' : 'usd';
  }

  onSell() {

    var ethAddress = this._ethService.getEthAddress();
    if (ethAddress == null) {
      this._messageBox.alert('Enable metamask first');
      return;
    }

    this.progress = true;
    this._apiService.goldSellReqest(ethAddress, this.to_sell)
      .finally(() => {
        this.progress = false;
        this._cdRef.detectChanges();
      })
      .subscribe(res => {
        this._messageBox.alert('Estimated USD amount is ' + res.data.fiatAmount);
        this._ethService.sendSellRequest(ethAddress, res.data.payload);
      },
      err => {
        if (err.error && err.error.errorCode) {
          this._messageBox.alert(err.error.errorDesc);
        }
      });
  }
}
