import {Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, OnDestroy} from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService } from '../../services';
import { GoldBuyResponse, GoldBuyDryResponse } from '../../interfaces'
import {Subscription} from 'rxjs/Subscription';

@Component({
  selector: 'app-buy-page',
  templateUrl: './buy-page.component.html',
  styleUrls: ['./buy-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'page' }
})
export class BuyPageComponent implements OnInit {

  progress: boolean;
  to_spend: number;
  estimate_amount: number;
  discount: number = 0;

  public buyCurrency:     'usd'|'gold' = 'usd';
  public resultCurrrency: 'usd'|'gold' = 'gold';

  usdBalance: number;
  goldUsdRate: number;
  estimatesAmount;
  usdBalancePercent;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this._ethService.checkWeb3();
    this._ethService.transferBalance$.subscribe((data) => {
      this.usdBalance = data['usd'];
    });
    this._apiService.getGoldRate().subscribe(data => {
      this.goldUsdRate = +data.data.rate;
    });
  }

  onToSpendChanged(value: number) {
    this.to_spend = value;
    this.estimatesAmount = this.estimatesAmountDecor((this.to_spend / this.goldUsdRate));

    this.estimate_amount = 0; // value && value > 0 ? value / 2 : 0;
    // this.discount = value && value > 0 ? Math.floor(Math.random() * 100) : 0;
    this._cdRef.detectChanges();
  }

  onSetBuyPercent(percent) {
    this.usdBalancePercent = this.usdBalance * percent;
    this.to_spend = this.usdBalancePercent;
    this.estimatesAmount = this.estimatesAmountDecor((this.usdBalancePercent / this.goldUsdRate));
  }

  estimatesAmountDecor(price) {
    return price.toFixed(2).replace(/(\d)(?=(\d\d\d)+([^\d]|$))/g, '$1 ');
  }

  onCurrencyChanged(value: string) {
    this.resultCurrrency = (value === 'usd') ? 'gold' : 'usd';
  }

  onBuy() {
    var ethAddress = this._ethService.getEthAddress();
    if (ethAddress == null) {
      this._messageBox.alert('Enable metamask first');
      return;
    }

    this.progress = true;
    this._apiService.goldBuyReqest(ethAddress, this.to_spend)
      .finally(() => {
        this.progress = false;
        this._cdRef.detectChanges();
      })
      .subscribe(res => {
        this._messageBox.alert('Estimated gold amount is ' + res.data.goldAmount);
        this._ethService.sendBuyRequest(ethAddress, res.data.payload);
      },
      err => {
        if (err.error && err.error.errorCode) {
          this._messageBox.alert(err.error.errorDesc);
        }
      });
  }
}
