import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding
} from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import * as Web3 from 'web3';
import { Subscription } from 'rxjs/Subscription';
import { FormGroup } from '@angular/forms';
import { Observable } from "rxjs/Observable";
import { BigNumber } from 'bignumber.js'
import { DecimalPipe } from "@angular/common";

@Component({
  selector: 'app-sell-page',
  templateUrl: './sell-page.component.html',
  styleUrls: ['./sell-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SellPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  locale: string;
  progress: boolean = false;
  confirmation: boolean = false;

  toSellUnset: boolean = true;
  toSell: BigNumber = new BigNumber(0);
  toSellVal: string = "";
  commission: number = 0;

  goldBalance: BigNumber = null;
  mntpBalance: BigNumber = null;
  goldUsdRate: number = 0;
  estimatedAmount: string = null;
 
  commissionArray: number[] = [3, 2.5, 1.5, 0.75];
  mntpArray: number[] = [10, 1000, 10000];
  buyMNT_DisableArray = [false, false, false, false];
  buyMNTArray = [10, 1000, 10000];
  discountUSDArray: number[] = [0, 0, 0, 0];
  public sellAmountChecked: boolean = true;
  public ethAddress: string = '';
  public selectedWallet = 0;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {

    this.toSellUnset = true;

    this._userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
      this._cdRef.markForCheck();
    });

    Observable.combineLatest(
      this._ethService.getObservableGoldBalance(),
      this._goldrateService.getObservableRate(),
      this._ethService.getObservableMntpBalance()
    )
      .subscribe((data) => {
        if (data[0] !== null) this.goldBalance = data[0];
        if (data[1] !== null) this.goldUsdRate = data[1];
        if (data[2] !== null) this.mntpBalance = data[2];

        // got gold balance first time
        if (this.goldBalance !== null && this.goldBalance.gt(0) && this.toSellUnset) {
          this.toSellUnset = false;
          this.toSell = this.goldBalance.decimalPlaces(6, BigNumber.ROUND_DOWN);
          this.toSellVal = this.toSell.toString();
        }

        // got all needed data to calculate estimated value
        this.recalcCommission(this.goldUsdRate, this.goldBalance, this.mntpBalance);

        // dont update values while user clicks primary button
        if (!this.progress && !this.confirmation) {
          this._cdRef.markForCheck();
        }
      });

    this._ethService.getObservableEthAddress().subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress) {
        this.selectedWallet = 0;
      } else {
        this.selectedWallet = 1;
      }
    });
  }

  onToSellChanged(value: string) {

    this.toSellUnset = false;
    var testValue = value != null && value.length > 0 ? new BigNumber(value) : new BigNumber(0);
    this.toSell = new BigNumber(0);

    if (testValue.gt(0)) {
      this.toSell = new BigNumber(value);
      this.toSell = this.toSell.decimalPlaces(6, BigNumber.ROUND_DOWN);
    }
    this.recalcCommission(this.goldUsdRate, this.goldBalance, this.mntpBalance);
    this._cdRef.markForCheck();
  }

  onSetSellPercent(percent:number) {
    var goldBalancePercent = new BigNumber(0);
    if (this.goldBalance != null) {
      goldBalancePercent = new BigNumber(this.goldBalance.times(percent));
    }
    this.onToSellChanged(goldBalancePercent.toString());
    this.toSellVal = this.toSell.toString();
    this._cdRef.markForCheck();
  }

  recalcCommission(rate: number, goldBalance: BigNumber, mntpBalance: BigNumber) {

    if (rate === 0) return;
    if (goldBalance == null) goldBalance = new BigNumber(0);

    // current comission
    var mntpNum = mntpBalance !== null ? mntpBalance.toNumber() : 0;
    if (mntpNum < this.mntpArray[0]) {
      this.commission = this.commissionArray[0];
    } else if (mntpNum >= this.mntpArray[0] && mntpNum < this.mntpArray[1]) {
      this.commission = this.commissionArray[1];
    } else if (mntpNum >= this.mntpArray[1] && mntpNum < this.mntpArray[2]) {
      this.commission = this.commissionArray[2];
    } else {
      this.commission = this.commissionArray[3];
    }

    this.updateDiscountBlockData(mntpNum);

    // get estimated cents (gross)
    const toConvert = BigNumber.min(this.toSell, goldBalance);
    const amountCentsGross = Math.floor(toConvert.times(this.goldUsdRate).times(100).toNumber());
    var amountNet:number = 0;

    // update all fee `levels` including current estimated amount
    for (let i = 0; i < this.discountUSDArray.length; i++) {
      const feeCents = Math.floor(this.commissionArray[i] * amountCentsGross / 100);
      this.discountUSDArray[i] = (amountCentsGross - feeCents) / 100;

      if (this.commissionArray[i] === this.commission) {
        amountNet = this.discountUSDArray[i];
        this.estimatedAmount = this.discountUSDArray[i].toFixed(2);
      }
    }

    // gold amount bounds and minimum 1 USD estimated
    if (!this.toSellUnset) {
      this.sellAmountChecked = this.toSell.gt(0) && this.toSell.lte(goldBalance) && amountNet >= 1;
    }
  }

  updateDiscountBlockData(mntp:number) {
    this.buyMNTArray = [10 - mntp, 1000 - mntp, 10000 - mntp];

    this.buyMNT_DisableArray[0] = false;
    this.buyMNT_DisableArray[1] = false;
    this.buyMNT_DisableArray[2] = false;

    if (mntp < this.mntpArray[0]) {
    } else if (mntp >= this.mntpArray[0] && mntp < this.mntpArray[1]) {
      this.buyMNT_DisableArray[0] = true;
    } else if (mntp >= this.mntpArray[1] && mntp < this.mntpArray[2]) {
      this.buyMNT_DisableArray[0] = true;
      this.buyMNT_DisableArray[1] = true;
    } else {
      this.buyMNT_DisableArray[0] = true;
      this.buyMNT_DisableArray[1] = true;
      this.buyMNT_DisableArray[2] = true;
    }
  }

  onSell() {
    this.progress = true;
    this._cdRef.markForCheck();

    if (this.selectedWallet == 0) {
      this._apiService.goldSellHwReqest(this.toSell)
        .finally(() => {
          this.progress = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
            const confText =
              "GOLD to sell: " +
              (new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18))) +
              " GOLD<br/>" +
              "You will get: $ " +
              res.data.fiatAmount +
              " ($ " +
              res.data.feeAmount +
              " fee)<br/>" +
              "GOLD/USD: $ " +
              res.data.goldRate;

            this.confirmation = true;
            this._cdRef.markForCheck();
            console.log(res);
            this._messageBox.confirm(confText).subscribe(ok => {
              this.confirmation = false;
              if (ok) {
                this._apiService.confirmHwReqest(false, res.data.requestId).subscribe((data) => {
                  console.log(data);
                },
                err => {
                  if (err.error && err.error.errorCode) {
                    this._messageBox.alert(err.error.errorDesc);
                  }
                });
              }
              this._cdRef.markForCheck();
            });
          },
          err => {
            if (err.error && err.error.errorCode) {
              this._messageBox.alert(err.error.errorDesc);
            }
          });
    } else {
      this._apiService.goldSellReqest(this.ethAddress, this.toSell)
        .finally(() => {
          this.progress = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
            var confText =
              "GOLD to sell: " +
              (new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18))) +
              " GOLD<br/>" +
              "You will get: $ " +
              res.data.fiatAmount +
              " ($ " +
              res.data.feeAmount +
              " fee)<br/>" +
              "GOLD/USD: $ " +
              res.data.goldRate;

            this.confirmation = true;
            this._cdRef.markForCheck();

            this._messageBox.confirm(confText).subscribe(ok => {
              this.confirmation = false;
              if (ok) {
                this._ethService.sendSellRequest(this.ethAddress, res.data.payload);
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
  }
}
