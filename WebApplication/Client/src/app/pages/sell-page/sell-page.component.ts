import {Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding} from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService } from '../../services';
import * as Web3 from 'web3';
import {Subscription} from 'rxjs/Subscription';
import {FormGroup} from '@angular/forms';

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

  goldBalance: number;
  goldUsdRate: number;
  estimatesAmount;
  goldBalancePercent;

  form: FormGroup;

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
      this.goldBalance = data['gold'];
    });
    this._apiService.getGoldRate().subscribe(data => {
      this.goldUsdRate = +data.data.rate;
    });
  }

  onToSellChanged(value: number) {
    this.to_sell = +value;


    this.estimatesAmount = this.estimatesAmountDecor((this.to_sell * this.goldUsdRate));

    this.estimate_amount = 0; // value * 2;
    // this.discount = value && value > 0 ? Math.floor(Math.random() * 100) : 0;
    this._cdRef.detectChanges();
  }

  onSetSellPercent(percent) {
    this.goldBalancePercent = this.goldBalance * percent;
    this.to_sell = this.goldBalancePercent;
    this.estimatesAmount = this.estimatesAmountDecor((this.goldBalancePercent * this.goldUsdRate));
  }

  estimatesAmountDecor(price) {
    return price.toFixed(2).replace(/(\d)(?=(\d\d\d)+([^\d]|$))/g, '$1 ');
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
    this._apiService.goldBuyReqest(ethAddress, this.to_sell)
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
