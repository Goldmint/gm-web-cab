import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding
} from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import * as Web3 from 'web3';
import { Subscription } from 'rxjs/Subscription';
import { FormGroup } from '@angular/forms';
import { Observable } from "rxjs/Observable";

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

  public sellCurrency: 'usd' | 'gold' = 'gold';
  public resultCurrrency: 'usd' | 'gold' = 'usd';

  goldBalance: number;
  mntpBalance: number;
  goldUsdRate: number;
  estimatesAmount;
  goldBalancePercent;
  stopUpdate = false;

  form: FormGroup;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    Observable.combineLatest(
      this._ethService.getObservableGoldBalance(),
      this._goldrateService.getObservableRate(),
      this._ethService.getObservableMntpBalance()
    )
      .subscribe((data) => {
        this.goldBalance = data[0];
        this.goldUsdRate = data[1];
        this.mntpBalance = data[2];
        if (!this.stopUpdate && this.goldBalance !== null) {
          this.onSetSellPercent(1);
          this.onToSellChanged(this.goldBalance);
          this.stopUpdate = true;
        }
      });

  }

  onToSellChanged(value: number) {
    this.to_sell = +value;
    this.calculationDiscount(this.mntpBalance);
    const amount = (this.to_sell * this.goldUsdRate) * ((100 - this.discount) / 100);
    this.estimatesAmount = this.estimatesAmountDecor(amount);

    this.estimate_amount = 0; // value * 2;
    // this.discount = value && value > 0 ? Math.floor(Math.random() * 100) : 0;
    this._cdRef.detectChanges();
  }

  onSetSellPercent(percent) {
    this.calculationDiscount(this.mntpBalance);
    this.goldBalancePercent = this.goldBalance * percent;
    this.to_sell = this.goldBalancePercent;
    const amount = (this.goldBalancePercent * this.goldUsdRate) * ((100 - this.discount) / 100);
    this.estimatesAmount = this.estimatesAmountDecor(amount);
  }

  estimatesAmountDecor(price) {
    return price.toFixed(2).replace(/(\d)(?=(\d\d\d)+([^\d]|$))/g, '$1 ');
  }

  onCurrencyChanged(value: string) {
    this.resultCurrrency = (value === 'usd') ? 'gold' : 'usd';
  }

  calculationDiscount(mntp) {
    if (mntp <= 10) {
      this.discount = 3;
    } else if (mntp > 10 && mntp <= 1000) {
      this.discount = 2.5;
    } else if (mntp > 1000 && mntp <= 10000) {
      this.discount = 1.5;
    } else {
      this.discount = 0.75;
    }
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
