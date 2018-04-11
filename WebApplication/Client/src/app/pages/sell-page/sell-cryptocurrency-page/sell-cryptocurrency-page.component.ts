import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {APIService, EthereumService, GoldrateService, MessageBoxService, UserService} from "../../../services";
import {TFAInfo} from "../../../interfaces";
import {Subscription} from "rxjs/Subscription";
import {User} from "../../../interfaces/user";
import {Subject} from "rxjs/Subject";
import {BigNumber} from "bignumber.js";
import {Observable} from "rxjs/Observable";
import {TranslateService} from "@ngx-translate/core";

@Component({
  selector: 'app-sell-cryptocurrency-page',
  templateUrl: './sell-cryptocurrency-page.component.html',
  styleUrls: ['./sell-cryptocurrency-page.component.sass']
})
export class SellCryptocurrencyPageComponent implements OnInit, OnDestroy {
  @HostBinding('class') class = 'page';

  public loading = false;
  public progress: boolean = false;
  public isBalancesLoaded: boolean = false;
  public confirmation: boolean = false;

  public user: User;
  public tfaInfo: TFAInfo;

  public goldUsdRate: number = 0;
  public goldBalance: BigNumber = null;
  public hotGoldBalance: BigNumber = null;
  public mntpBalance: BigNumber = null;
  public ethAddress: string = '';
  public selectedWallet = 0;

  private sub1: Subscription;

  // public toSell: BigNumber = new BigNumber(0);
  // public toSellVal: string = "";
  // public commission: number = 0;
  // public estimatedAmount: string = null;
  // public commissionArray: number[] = [3, 2.5, 1.5, 0.75];
  // public mntpArray: number[] = [10, 1000, 10000];
  // public buyMNT_DisableArray = [false, false, false, false];
  // public buyMNTArray = [10, 1000, 10000];
  // public discountUSDArray: number[] = [0, 0, 0, 0];
  // public sellAmountChecked: boolean = true;

  public coinList = ['btc', 'eth']
  public currentCoin = this.coinList[1];
  public cCurrencyGoldAmount: any = null;
  public cCurrencyAmountView = 0;
  public cCurrencyEstimateAmount = 0;
  public ethRate: number = 450;
  public invalidBalance: boolean = false;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef,
    private _translate: TranslateService
  ) { }

  ngOnInit() {
    Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._userService.currentUser
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1];
        this.loading = false;
        this._cdRef.detectChanges();
      });

    Observable.combineLatest(
      this._ethService.getObservableGoldBalance(),
      this._ethService.getObservableMntpBalance()
      // this._apiService.getEthereumRate()
    )
      .takeUntil(this.destroy$).subscribe((data) => {
      if (data[0] !== null && (this.goldBalance === null || !this.goldBalance.eq(data[0]))
        && data[1] !== null && (this.mntpBalance === null || !this.mntpBalance.eq(data[0]))
      ) {
        this.goldBalance = data[0];
        this.mntpBalance = data[1];
        this.setCCurrencyGoldBalance();
      }
      // this.ethRate = data[2].data.usd;
    });

    this._ethService.getObservableHotGoldBalance().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null && (this.hotGoldBalance === null || !this.hotGoldBalance.eq(data))) {
        this.hotGoldBalance = data;
        this.setCCurrencyGoldBalance();
      }
    });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(rate => {
      if (rate > 0 && rate !== this.goldUsdRate) {
        this.goldUsdRate = rate;
        if (this.cCurrencyGoldAmount !== null) {
          this.cCurrencyEstimateAmount = this.calculationCCurrencyAmount();
          this._cdRef.markForCheck();
        }
      }
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress) {
        this.selectedWallet = 0;
        this.setCCurrencyGoldBalance();
      }
    });

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;
    this.setCCurrencyGoldBalance();

    this._userService.onWalletSwitch$.takeUntil(this.destroy$).subscribe((wallet) => {
      this.selectedWallet = wallet['id'] === 'hot' ? 0 : 1;
      this.setCCurrencyGoldBalance();
    });

  }

  chooseCurrentCoin(coin) {
    if (this.currentCoin !== coin) {
      this.currentCoin = coin;
    }
  }

  onCCurrencyChanged(value: string) {
    this.invalidBalance = false;
    const testValue = value != null && value.length > 0 ? new BigNumber(value) : new BigNumber(0);
    this.cCurrencyGoldAmount = new BigNumber(0);

    if (testValue.gt(0)) {
      this.cCurrencyGoldAmount = new BigNumber(value);
      this.cCurrencyGoldAmount = this.cCurrencyGoldAmount.decimalPlaces(6, BigNumber.ROUND_DOWN);
    }
    this.cCurrencyEstimateAmount = this.calculationCCurrencyAmount();
    if (this.selectedWallet === 0 && +value > this.hotGoldBalance.toNumber()) {
      this.invalidBalance = true;
    } else if (this.selectedWallet === 1 && +value > this.goldBalance.toNumber()) {
      this.invalidBalance = true;
    }

    this._cdRef.markForCheck();
  }

  setCCurrencyGoldBalance(percent: number = 1) {
    let goldBalance = this.selectedWallet == 0 ? this.hotGoldBalance : this.goldBalance;
    if (!goldBalance) {
      return;
    }
    goldBalance = new BigNumber(goldBalance.times(percent));
    // got gold balance first time
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

  onCryptoCurrencySubmit() {
    this.loading = true;
    if (this.selectedWallet == 0) {

    } else {
      this._apiService.goldSellAsset(this.ethAddress, this.cCurrencyGoldAmount)
        .finally(() => {
          this.loading = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
          const amount = +(this.cCurrencyGoldAmount * (res.data.goldRate /res.data.ethRate)).toFixed(6);
          this._translate.get('MessageBox.EthWithdraw',
            {coinAmount: this.cCurrencyGoldAmount, goldAmount: amount, ethRate: res.data.ethRate}
          ).subscribe(phrase => {
            this._messageBox.confirm(phrase).subscribe(ok => {
              if (ok) {
                this._apiService.goldSellConfirm(res.data.requestId).subscribe(() => {
                  console.log('confirmed');
                  // this._ethService.sendBuyRequest(this.ethAddress, res.data.requestId, this.cCurrencyCoinAmount);
                });
              }
            });
          });
        });
    }
  }

  // onSell() {
  //   this.progress = true;
  //   this._cdRef.markForCheck();
  //
  //   if (this.selectedWallet == 0) {
  //     this._apiService.goldSellHwRequest(this.toSell)
  //       .finally(() => {
  //         this.progress = false;
  //         this._cdRef.markForCheck();
  //       })
  //       .subscribe(res => {
  //         const amount = new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18))
  //         this.confirmation = true;
  //         this._cdRef.markForCheck();
  //
  //         this._translate.get('MessageBox.GoldSell',
  //           {goldAmount: amount, fiatAmount: res.data.fiatAmount, feeAmount: res.data.feeAmount, goldRate: res.data.goldRate}
  //         ).subscribe(phrase => {
  //           this._messageBox.confirm(phrase).subscribe(ok => {
  //             this.confirmation = false;
  //             if (ok) {
  //               this._apiService.confirmHwRequest(false, res.data.requestId).subscribe(() => {
  //                 this._translate.get('MessageBox.RequestProgress' ).subscribe(phrase => {
  //                   this._messageBox.alert(phrase).subscribe(() => {
  //                     this.router.navigate(['/finance/history']);
  //                   });
  //                 });
  //               });
  //             }
  //             this._cdRef.markForCheck();
  //           });
  //         });
  //       });
  //   } else {
  //     this._apiService.goldSellRequest(this.ethAddress, this.toSell)
  //       .finally(() => {
  //         this.progress = false;
  //         this._cdRef.markForCheck();
  //       })
  //       .subscribe(res => {
  //         const amount = new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18))
  //         this.confirmation = true;
  //         this._cdRef.markForCheck();
  //
  //         this._translate.get('MessageBox.GoldSell',
  //           {goldAmount: amount, fiatAmount: res.data.fiatAmount, feeAmount: res.data.feeAmount, goldRate: res.data.goldRate}
  //         ).subscribe(phrase => {
  //           this._messageBox.confirm(phrase).subscribe(ok => {
  //             this.confirmation = false;
  //             if (ok) {
  //               this._apiService.confirmMMRequest(false, res.data.requestId).subscribe(() => {
  //                 // this._ethService.sendSellRequest(this.ethAddress, res.data.payload);
  //               });
  //             }
  //             this._cdRef.markForCheck();
  //           });
  //         });
  //       });
  //   }
  // }

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

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
