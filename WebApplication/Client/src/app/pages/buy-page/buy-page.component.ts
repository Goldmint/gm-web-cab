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
  toSpendVal: string = "";
  toSpend: BigNumber = new BigNumber(0);

  usdBalance: number = null;
  goldUsdRate: number = null;
  estimatedAmount: string = "";
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

        // required values are provided
        if (this.goldUsdRate !== null && this.usdBalance !== null) {

          // got first time
          if (this.toSpendUnset && this.usdBalance > 0) {
            this.toSpendUnset = false;
            this.toSpend = new BigNumber(this.usdBalance);
            this.toSpendVal = this.toSpend.toString();
            this.buyAmountCheck(this.toSpend);
          }

          if (!this.progress && !this.confirmation) {
            this.estimate(this.toSpend);
          }

          this._cdRef.markForCheck();
        }
      });
  }

  onToSpendChanged(value: string) {
    this.toSpendUnset = false;

    this.toSpend = new BigNumber(0);
    var testVal = this.usdBalance && value && value.length > 0 ? parseFloat(value): 0;

    if (testVal > 0) {
      this.toSpend = (new BigNumber(testVal)).decimalPlaces(2, BigNumber.ROUND_DOWN);
    }

    this.estimate(this.toSpend);
    this.buyAmountCheck(this.toSpend);
    this._cdRef.markForCheck();
  }

  estimate(amount: BigNumber) {
    var toConvert = this.usdBalance ? BigNumber.min(amount, new BigNumber(this.usdBalance)) : new BigNumber(0);
    this.estimatedAmount = toConvert.dividedBy(this.goldUsdRate).toPrecision(18 + 1);
    this.estimatedAmount = this.estimatedAmount.substr(0, this.estimatedAmount.length - 1);
  }

  onBuy() {

    var ethAddress = this._ethService.getEthAddress();
    if (ethAddress == null) {
      this._messageBox.alert('Enable metamask first');
      return;
    }

    this.progress = true;
    this._cdRef.markForCheck();

    this._apiService.goldBuyReqest(ethAddress, this.toSpend.toNumber())
      .finally(() => {
        this.progress = false;
        this._cdRef.markForCheck();
      })
      .subscribe(res => {
        var confText =
          "USD to spend: " + this.toSpend + "<br/>" +
          "You will get: " + (new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18))) + " GOLD<br/>" +
          "GOLD/USD: $ " + res.data.goldRate
          ;

        this.confirmation = true;
        this._cdRef.markForCheck();

        this._messageBox.confirm(confText).subscribe(ok => {
          this.confirmation = false;
          if (ok) {
            this._ethService.sendBuyRequest(ethAddress, res.data.payload);
          }
          this._cdRef.markForCheck();
        });
      },
      err => {
        if (err.error && err.error.errorCode) {
          this._messageBox.alert(err.error.errorDesc);
        }
      });
  }

  buyAmountCheck(val: BigNumber) {
    this.buyAmountChecked = val.gte(1) && this.usdBalance && val.lte(this.usdBalance);
  }
}
