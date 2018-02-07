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
  commission: number = 0;

  public sellCurrency: 'usd' | 'gold' = 'gold';
  public resultCurrrency: 'usd' | 'gold' = 'usd';

  goldBalance: number;
  mntpBalance: number;
  goldUsdRate: number;
  estimatesAmount;
  goldBalancePercent;
  stopUpdate = false;

  commissionArray: number[] = [3, 2.5, 1.5, 0.75];
  mntpArray: number[] = [10, 1000, 10000];
  buyMNT_DisableArray = [false, false, false, false];
  buyMNTArray = [10, 1000, 10000];
  discountUSDArray: number[] = [0, 1, 2, 3];

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
        if (this.stopUpdate && this.goldUsdRate !== data[1]) {
          this.goldUsdRate = data[1];
          this.onToSellChanged(this.to_sell);
        } else if (this.stopUpdate && this.goldBalance !== data[0]) {
          this.goldBalance = data[0];
          this.onSetSellPercent(1);
        } else if (this.mntpBalance && this.mntpBalance !== data[2]) {
          this.mntpBalance = data[2];
          this.onToSellChanged(this.to_sell);
        }

        this.goldBalance = data[0];
        this.goldUsdRate = data[1];
        this.mntpBalance = data[2];

        if (!this.stopUpdate && this.goldBalance !== null && this.goldUsdRate !== null && this.mntpBalance !== null) {
          this.onSetSellPercent(1);
          this.onToSellChanged(this.goldBalance);
          this.stopUpdate = true;
        }
      });

  }

  onToSellChanged(value: number) {
    this.to_sell = +value;
    this.getDataCommission(this.goldUsdRate, this.goldBalance, this.mntpBalance);

    this.estimate_amount = 0; // value * 2;
    // this.discount = value && value > 0 ? Math.floor(Math.random() * 100) : 0;
    this._cdRef.detectChanges();
  }

  onSetSellPercent(percent) {
    this.goldBalancePercent = this.goldBalance * percent;
    this.to_sell = this.goldBalancePercent;
    this.getDataCommission(this.goldUsdRate, this.goldBalance, this.mntpBalance);
  }

  estimatesAmountDecor(price) {
    return price.toFixed(2).replace(/(\d)(?=(\d\d\d)+([^\d]|$))/g, '$1 ');
  }

  onCurrencyChanged(value: string) {
    this.resultCurrrency = (value === 'usd') ? 'gold' : 'usd';
  }

  getDataCommission(rate, gold, mntp) {
    this.calculationDiscount(mntp);
    this.calculationData(mntp);
    const amount = this.to_sell * this.goldUsdRate;

    for (let i = 0; i < this.discountUSDArray.length; i++) {
      const amountCommission = amount * ((100 - this.commissionArray[i]) / 100);
      this.discountUSDArray[i] = +(amount - amountCommission).toFixed(2);
      if (this.commissionArray[i] === this.commission) {
        this.estimatesAmount = this.estimatesAmountDecor(amountCommission);
      }
    }
  }

  calculationDiscount(mntp) {
    if (mntp < this.mntpArray[0]) {
      this.commission = this.commissionArray[0];
    } else if (mntp >= this.mntpArray[0] && mntp < this.mntpArray[1]) {
      this.commission = this.commissionArray[1];
    } else if (mntp >= this.mntpArray[1] && mntp < this.mntpArray[2]) {
      this.commission = this.commissionArray[2];
    } else {
      this.commission = this.commissionArray[3];
    }
  }

  calculationData(mntp) {
    this.buyMNTArray = [10, 1000, 10000];
    this.buyMNT_DisableArray[0] = false;
    this.buyMNT_DisableArray[1] = false;
    this.buyMNT_DisableArray[2] = false;

    if (mntp < this.mntpArray[0]) {
      this.buyMNTArray[0] = this.mntpArray[0] -  mntp;
    } else if (mntp >= this.mntpArray[0] && mntp < this.mntpArray[1]) {
      this.buyMNTArray[1] = this.mntpArray[1] - mntp;
      this.buyMNT_DisableArray[0] = true;
    } else if (mntp >= this.mntpArray[1] && mntp < this.mntpArray[2]) {
      this.buyMNTArray[2] = this.mntpArray[2] -  mntp;
      this.buyMNT_DisableArray[0] = true;
      this.buyMNT_DisableArray[1] = true;
    } else {
      this.buyMNT_DisableArray[0] = true;
      this.buyMNT_DisableArray[1] = true;
      this.buyMNT_DisableArray[2] = true;
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
