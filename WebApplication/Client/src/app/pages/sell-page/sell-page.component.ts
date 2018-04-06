import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding, OnDestroy
} from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import { Observable } from "rxjs/Observable";
import { BigNumber } from 'bignumber.js';
import {Subscription} from "rxjs/Subscription";
import {Router} from "@angular/router";
import {TranslateService} from "@ngx-translate/core";

enum Pages { CryptoCurrency,  CryptoCapital }

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

  // --------------

  public pages = Pages;
  public page: Pages;

  public loading = false;

  public coinList = ['btc', 'eth']
  public currentCoin = this.coinList[1];
  public cCurrencyGoldAmount: any = null;
  public cCurrencyAmountView = 0;
  public cCurrencyEstimateAmount = 0;
  public ethRate: number;
  public invalidBalance: boolean = false;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef,
    private router: Router,
    private _translate: TranslateService
  ) { }

  ngOnInit() {
    this.page = Pages.CryptoCurrency;

    this._userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
      this._cdRef.markForCheck();
    });

    Observable.combineLatest(
      this._ethService.getObservableGoldBalance(),
      this._ethService.getObservableMntpBalance(),
      this._apiService.getEthereumRate()
    )
      .subscribe((data) => {
        if (data[0] !== null && (this.goldBalance === null || !this.goldBalance.eq(data[0]))
          && data[1] !== null && (this.mntpBalance === null || !this.mntpBalance.eq(data[0]))
        ) {
          this.goldBalance = data[0];
          this.mntpBalance = data[1];
          this.ethRate = data[2].data.usd;
          this.setCCurrencyGoldBalance();
        }
      });

    this._ethService.getObservableHotGoldBalance().subscribe(data => {
        if (data !== null && (this.hotGoldBalance === null || !this.hotGoldBalance.eq(data))) {
          this.hotGoldBalance = data;
          this.setCCurrencyGoldBalance();
        }
      });

    this._goldrateService.getObservableRate().subscribe(rate => {
      if (rate > 0 && rate !== this.goldUsdRate) {
        this.goldUsdRate = rate;
        if (this.cCurrencyGoldAmount !== null) {
          this.cCurrencyEstimateAmount = this.calculationCCurrencyAmount();
          this._cdRef.markForCheck();
        }
      }
    });

    this._ethService.getObservableEthAddress().subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress) {
        this.selectedWallet = 0;
      }
      this.isBalancesLoaded = false;
      this.setCCurrencyGoldBalance();
    });

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;
    this.setCCurrencyGoldBalance();

    this.sub1 = this._userService.onWalletSwitch$.subscribe((wallet) => {
      if (wallet['id'] === 'hot') {
        this.selectedWallet = 0;
      } else {
        this.selectedWallet = 1;
      }
      this.setCCurrencyGoldBalance();
    });
  }

  onCCurrencyChanged(value: string) {
    const testValue = value != null && value.length > 0 ? new BigNumber(value) : new BigNumber(0);
    this.cCurrencyGoldAmount = new BigNumber(0);

    if (testValue.gt(0)) {
      this.cCurrencyGoldAmount = new BigNumber(value);
      this.cCurrencyGoldAmount = this.cCurrencyGoldAmount.decimalPlaces(6, BigNumber.ROUND_DOWN);
    }
    this.cCurrencyEstimateAmount = this.calculationCCurrencyAmount();
    this._cdRef.markForCheck();
  }

  setCCurrencyGoldBalance(percent: number = 1) {
    let goldBalance = this.selectedWallet == 0 ? this.hotGoldBalance : this.goldBalance;
    if (!goldBalance) {
      return;
    }
    goldBalance = new BigNumber(goldBalance.times(percent));
    // got gold balance first time
    this.isBalancesLoaded = true;
    if (goldBalance.gt(0)) {
      this.cCurrencyGoldAmount = goldBalance.decimalPlaces(6, BigNumber.ROUND_DOWN);
      this.cCurrencyEstimateAmount = this.calculationCCurrencyAmount();
      this.cCurrencyAmountView = this.cCurrencyGoldAmount.toNumber();
      this.invalidBalance = false;
    } else {
      this.cCurrencyEstimateAmount = this.cCurrencyAmountView = 0;
      this.cCurrencyGoldAmount = new BigNumber(0);
      this.invalidBalance = true;
    }
    this._cdRef.markForCheck();
  }

  calculationCCurrencyAmount() {
     return +(this.cCurrencyGoldAmount.toNumber() * (this.goldUsdRate / this.ethRate)).toFixed(6)
  }

  // recalcCommission() {
  //   if (!this.goldUsdRate) {
  //     return;
  //   }
  //
  //   let goldBalance = this.selectedWallet == 0 ? this.hotGoldBalance : this.goldBalance;
  //   goldBalance === null && (goldBalance = new BigNumber(0));
  //
  //   // current comission
  //   var mntpNum = this.mntpBalance !== null ? this.mntpBalance.toNumber() : 0;
  //   if (mntpNum < this.mntpArray[0]) {
  //     this.commission = this.commissionArray[0];
  //   } else if (mntpNum >= this.mntpArray[0] && mntpNum < this.mntpArray[1]) {
  //     this.commission = this.commissionArray[1];
  //   } else if (mntpNum >= this.mntpArray[1] && mntpNum < this.mntpArray[2]) {
  //     this.commission = this.commissionArray[2];
  //   } else {
  //     this.commission = this.commissionArray[3];
  //   }
  //
  //   this.updateDiscountBlockData(mntpNum);
  //
  //   // get estimated cents (gross)
  //   const toConvert = BigNumber.min(this.toSell, goldBalance);
  //   const amountCentsGross = Math.floor(toConvert.times(this.goldUsdRate).times(100).toNumber());
  //   var amountNet:number = 0;
  //
  //   // update all fee `levels` including current estimated amount
  //   for (let i = 0; i < this.discountUSDArray.length; i++) {
  //     const feeCents = Math.floor(this.commissionArray[i] * amountCentsGross / 100);
  //     this.discountUSDArray[i] = (amountCentsGross - feeCents) / 100;
  //
  //     if (this.commissionArray[i] === this.commission) {
  //       amountNet = this.discountUSDArray[i];
  //       this.estimatedAmount = this.discountUSDArray[i].toFixed(2);
  //     }
  //   }
  //
  //   // gold amount bounds and minimum 1 USD estimated
  //   this.sellAmountChecked = !this.isBalancesLoaded || (this.toSell.gt(0) && this.toSell.lte(goldBalance) && amountNet >= 1);
  //
  //   this._cdRef.markForCheck();
  // }
  //
  // updateDiscountBlockData(mntp:number) {
  //   this.buyMNTArray = [10 - mntp, 1000 - mntp, 10000 - mntp];
  //
  //   this.buyMNT_DisableArray[0] = false;
  //   this.buyMNT_DisableArray[1] = false;
  //   this.buyMNT_DisableArray[2] = false;
  //
  //   if (mntp < this.mntpArray[0]) {
  //   } else if (mntp >= this.mntpArray[0] && mntp < this.mntpArray[1]) {
  //     this.buyMNT_DisableArray[0] = true;
  //   } else if (mntp >= this.mntpArray[1] && mntp < this.mntpArray[2]) {
  //     this.buyMNT_DisableArray[0] = true;
  //     this.buyMNT_DisableArray[1] = true;
  //   } else {
  //     this.buyMNT_DisableArray[0] = true;
  //     this.buyMNT_DisableArray[1] = true;
  //     this.buyMNT_DisableArray[2] = true;
  //   }
  // }

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
          const amount = new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18))
          this.confirmation = true;
          this._cdRef.markForCheck();

          this._translate.get('MessageBox.GoldSell',
            {goldAmount: amount, fiatAmount: res.data.fiatAmount, feeAmount: res.data.feeAmount, goldRate: res.data.goldRate}
            ).subscribe(phrase => {
              this._messageBox.confirm(phrase).subscribe(ok => {
                this.confirmation = false;
                if (ok) {
                  this._apiService.confirmHwRequest(false, res.data.requestId).subscribe(() => {
                    this._translate.get('MessageBox.RequestProgress' ).subscribe(phrase => {
                      this._messageBox.alert(phrase).subscribe(() => {
                        this.router.navigate(['/finance/history']);
                      });
                    });
                  });
                }
                this._cdRef.markForCheck();
              });
            });
          });
    } else {
      this._apiService.goldSellRequest(this.ethAddress, this.toSell)
        .finally(() => {
          this.progress = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
          const amount = new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18))
          this.confirmation = true;
          this._cdRef.markForCheck();

          this._translate.get('MessageBox.GoldSell',
            {goldAmount: amount, fiatAmount: res.data.fiatAmount, feeAmount: res.data.feeAmount, goldRate: res.data.goldRate}
          ).subscribe(phrase => {
              this._messageBox.confirm(phrase).subscribe(ok => {
                this.confirmation = false;
                if (ok) {
                  this._apiService.confirmMMRequest(false, res.data.requestId).subscribe(() => {
                    this._ethService.sendSellRequest(this.ethAddress, res.data.payload);
                  });
                }
                this._cdRef.markForCheck();
              });
            });
          });
    }
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }

}
