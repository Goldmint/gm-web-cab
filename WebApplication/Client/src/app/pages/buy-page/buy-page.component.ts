import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import { GoldBuyResponse } from '../../interfaces'
import { Subscription } from 'rxjs/Subscription';
import { Observable } from "rxjs/Observable";
import { BigNumber } from 'bignumber.js'

@Component({
  selector: 'app-buy-page',
  templateUrl: './buy-page.component.html',
  styleUrls: ['./buy-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'page' }
})
export class BuyPageComponent implements OnInit {

  confirmation: boolean = false;
  progress: boolean = false;
  toSpendUnset: boolean = true;
  toSpend: number = 1;

  usdBalance: number = null;
  goldUsdRate: number = null;
  estimatedAmount: string;
  usdBalancePercent;
  public buyAmountChecked: boolean = true;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    Observable.combineLatest(this._ethService.getObservableUsdBalance(), this._goldrateService.getObservableRate())
      .subscribe((data) => {
        if (data[0] !== null) this.usdBalance = data[0];
        if (data[1] !== null) this.goldUsdRate = data[1];

        if (this.goldUsdRate !== null && this.usdBalance !== null) {
          if (this.toSpendUnset && data[0] > 0) {
            this.toSpend = this.usdBalance;
            this.toSpendUnset = false;
            this.buyAmountCheck(this.toSpend);
          }

          if (!this.progress && !this.confirmation) {
            this.estimate(this.toSpend);
          }

          this._cdRef.detectChanges();
        }
      });
  }

  onToSpendChanged(value: number) {
    this.toSpendUnset = false;
    this.estimate(this.toSpend);
    this.buyAmountCheck(value);
    this._cdRef.detectChanges();
  }

  estimate(amount: number) {
    this.estimatedAmount = (new BigNumber(amount)).dividedBy(this.goldUsdRate).toPrecision(18 + 1);
    this.estimatedAmount = this.estimatedAmount.substr(0, this.estimatedAmount.length - 1);
  }

  onBuy() {

    var ethAddress = this._ethService.getEthAddress();
    if (ethAddress == null) {
      this._messageBox.alert('Enable metamask first');
      return;
    }

    this.progress = true;
    this._cdRef.detectChanges();

    this._apiService.goldBuyReqest(ethAddress, this.toSpend)
      .finally(() => {
        this.progress = false;
        this._cdRef.detectChanges();
      })
      .subscribe(res => {
        var confText =
          "USD to spend: " + this.toSpend + "<br/>" +
          "You will get: " + (new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18))) + " GOLD<br/>" +
          "GOLD/USD: $ " + res.data.goldRate
          ;

        this.confirmation = true;
        this._cdRef.detectChanges();

        this._messageBox.confirm(confText).subscribe(ok => {
          this.confirmation = false;
          if (ok) {
            this._ethService.sendBuyRequest(ethAddress, res.data.payload);
          }
          this._cdRef.detectChanges();
        });
      },
      err => {
        if (err.error && err.error.errorCode) {
          this._messageBox.alert(err.error.errorDesc);
        }
      });
  }

  buyAmountCheck(val) {
    this.buyAmountChecked = val >= 1 && this.usdBalance && val<= this.usdBalance;
  }
}
