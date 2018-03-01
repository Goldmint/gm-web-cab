import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding, OnDestroy
} from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import { Observable } from "rxjs/Observable";
import { BigNumber } from 'bignumber.js';
import {Subscription} from "rxjs/Subscription";
import {Router} from "@angular/router";

@Component({
  selector: 'app-sell-page',
  templateUrl: './sell-page.component.html',
  styleUrls: ['./sell-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SellPageComponent implements OnInit, OnDestroy {
  @HostBinding('class') class = 'page';

  locale: string;
  progress: boolean = false;
  confirmation: boolean = false;

  isBalancesLoaded: boolean = false;
  toSell: BigNumber = new BigNumber(0);
  toSellVal: string = "";
  commission: number = 0;

  goldBalance: BigNumber = null;
  hotGoldBalance: BigNumber = null;
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

  private sub1: Subscription;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef,
    private router: Router
  ) { }

  ngOnInit() {
    this._userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
      this._cdRef.markForCheck();
    });

    Observable.combineLatest(
      this._ethService.getObservableGoldBalance(),
      this._ethService.getObservableMntpBalance()
    )
      .subscribe((data) => {
        if (data[0] !== null && (this.goldBalance === null || !this.goldBalance.eq(data[0]))
          && data[1] !== null && (this.mntpBalance === null || !this.mntpBalance.eq(data[0]))
        ) {
          this.goldBalance = data[0];
          this.mntpBalance = data[1];
          this.setGoldBalance();
        }
      });

    this._ethService.getObservableHotGoldBalance().subscribe(data => {
        if (data !== null && (this.hotGoldBalance === null || !this.hotGoldBalance.eq(data))) {
          this.hotGoldBalance = data;
          this.setGoldBalance();
        }
      });

    this._goldrateService.getObservableRate().subscribe(rate => {
      if (rate > 0 && rate !== this.goldUsdRate) {
        this.goldUsdRate = rate;
        this.recalcCommission();
      }
    });

    this._ethService.getObservableEthAddress().subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress) {
        this.selectedWallet = 0;
      }
      this.isBalancesLoaded = false;
      this.setGoldBalance();
    });

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;

    this.sub1 = this._userService.onWalletSwitch$.subscribe((wallet) => {
      this.selectedWallet = wallet['id'] === 'hot' ? 0 : 1;
      this._cdRef.markForCheck();
    });
  }

  setGoldBalance(percent: number = 1) {
    let goldBalance = this.selectedWallet == 0 ? this.hotGoldBalance : this.goldBalance;

    if (!goldBalance) {
      return;
    }

    goldBalance = new BigNumber(goldBalance.times(percent));

    // got gold balance first time
    this.isBalancesLoaded = true;
    if (goldBalance.gt(0)) {
      this.toSell = goldBalance.decimalPlaces(6, BigNumber.ROUND_DOWN);
      this.toSellVal = this.toSell.toString();
    }

    // got all needed data to calculate estimated value
    this.recalcCommission();

    // dont update values while user clicks primary button
    if (!this.progress && !this.confirmation) {
      this._cdRef.markForCheck();
    }
  }

  onToSellChanged(value: string) {
    var testValue = value != null && value.length > 0 ? new BigNumber(value) : new BigNumber(0);
    this.toSell = new BigNumber(0);

    if (testValue.gt(0)) {
      this.toSell = new BigNumber(value);
      this.toSell = this.toSell.decimalPlaces(6, BigNumber.ROUND_DOWN);
    }
    this.recalcCommission();
    this._cdRef.markForCheck();
  }

  recalcCommission() {
    if (!this.goldUsdRate) {
      return;
    }

    let goldBalance = this.selectedWallet == 0 ? this.hotGoldBalance : this.goldBalance;
    goldBalance === null && (goldBalance = new BigNumber(0));

    // current comission
    var mntpNum = this.mntpBalance !== null ? this.mntpBalance.toNumber() : 0;
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
    this.sellAmountChecked = !this.isBalancesLoaded || (this.toSell.gt(0) && this.toSell.lte(goldBalance) && amountNet >= 1);

    this._cdRef.markForCheck();
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
      this._apiService.goldSellHwRequest(this.toSell)
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

            this._messageBox.confirm(confText).subscribe(ok => {
              this.confirmation = false;
              if (ok) {
                this._apiService.confirmHwRequest(false, res.data.requestId).subscribe(() => {
                    this._messageBox.alert('Your request is in progress now!');
                    // this.router.navigate(['/finance/history']);
                },
                err => {
                  err.error && err.error.errorCode && this._messageBox.alert(err.error.errorCode == 1010
                    ? 'You have exceeded request frequency (One request for 30 minutes). Please try later'
                    : err.error.errorDesc
                  )
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
      this._apiService.goldSellRequest(this.ethAddress, this.toSell)
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

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }

}
