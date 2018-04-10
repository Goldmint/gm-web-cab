import {ChangeDetectorRef, Component, HostBinding, OnInit, ViewChild} from '@angular/core';
import {TranslateService} from "@ngx-translate/core";
import {APIService, EthereumService, GoldrateService, MessageBoxService, UserService} from "../../../services";
import {Observable} from "rxjs/Observable";
import {TFAInfo} from "../../../interfaces";
import {User} from "../../../interfaces/user";
import {BigNumber} from "bignumber.js";
import {Subject} from "rxjs/Subject";
import {Router} from "@angular/router";

@Component({
  selector: 'app-buy-cryptocurrency-page',
  templateUrl: './buy-cryptocurrency-page.component.html',
  styleUrls: ['./buy-cryptocurrency-page.component.sass']
})
export class BuyCryptocurrencyPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  @ViewChild('inputToSpend') inputToSpend;

  public loading = false;
  public confirmation: boolean = false;
  public progress: boolean = false;

  public goldUsdRate: number = 0;
  public ethAddress: string = '';
  public selectedWallet = 0;

  public user: User;
  public tfaInfo: TFAInfo;

  public coinList = ['btc', 'eth']
  public currentCoin = this.coinList[1];
  public ethRate: number = 450;
  public ethBalance: BigNumber = null;
  public hotEthBalance: BigNumber = null;
  public cCurrencyCoinAmount: any = null;
  public cCurrencyAmountView;
  public cCurrencyEstimateAmount;
  public invalidBalance = false;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef,
    private _translate: TranslateService,
    private router: Router
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

    /*Observable.combineLatest(
      this._ethService.getObservableGoldBalance(),
      this._ethService.getObservableMntpBalance()
      // this._apiService.getEthereumRate()
    )
      .subscribe((data) => {
        if (data[0] !== null && (this.goldBalance === null || !this.goldBalance.eq(data[0]))
          && data[1] !== null && (this.mntpBalance === null || !this.mntpBalance.eq(data[0]))
        ) {
          this.goldBalance = data[0];
          this.mntpBalance = data[1];
          this.setCCurrencyGoldBalance();
        }
        // this.ethRate = data[2].data.usd;
      });*/

    this._ethService.getObservableEthBalance().takeUntil(this.destroy$).subscribe(balance => {
      if (this.ethBalance === null || !this.ethBalance.eq(balance)) {
        this.ethBalance = balance;
        this.setCCurrencyEthBalance();
      }
    });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(rate => {
      if (rate > 0 && rate !== this.goldUsdRate) {
        this.goldUsdRate = rate;
        if (this.cCurrencyCoinAmount !== null) {
          this.cCurrencyEstimateAmount = this.calculationCCurrencyAmount();
          this._cdRef.markForCheck();
        }
      }
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress) {
        this.selectedWallet = 0;
        this.router.navigate(['buy']);
        this.setCCurrencyEthBalance();
      }
    });

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;
    this.setCCurrencyEthBalance();

    this._userService.onWalletSwitch$.takeUntil(this.destroy$).subscribe((wallet) => {
      if (wallet['id'] === 'hot') {
        this.selectedWallet = 0;
        this.router.navigate(['buy']);
      } else {
        this.selectedWallet = 1;
        this.setCCurrencyEthBalance();
      }
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
    this.cCurrencyCoinAmount = new BigNumber(0);

    if (testValue.gt(0)) {
      this.cCurrencyCoinAmount = new BigNumber(value);
      this.cCurrencyCoinAmount = this.cCurrencyCoinAmount.decimalPlaces(6, BigNumber.ROUND_DOWN);
    }
    this.cCurrencyEstimateAmount = this.calculationCCurrencyAmount();
    if (+value > this.ethBalance.toNumber()) {
      this.invalidBalance = true;
    }
    this._cdRef.markForCheck();
  }

  setCCurrencyEthBalance(percent: number = 1) {
    let ethBalance = this.selectedWallet == 0 ? this.hotEthBalance : this.ethBalance;
    if (!ethBalance) {
      return;
    }
    ethBalance = new BigNumber(ethBalance.times(percent));
    // got gold balance first time
    if (ethBalance.gt(0)) {
      this.cCurrencyCoinAmount = ethBalance.decimalPlaces(6, BigNumber.ROUND_DOWN);
      this.cCurrencyEstimateAmount = this.calculationCCurrencyAmount();
      this.cCurrencyAmountView = this.cCurrencyCoinAmount.toNumber();
      this.invalidBalance = false;
    } else {
      this.cCurrencyEstimateAmount = this.cCurrencyAmountView = 0;
      this.invalidBalance = true;
    }
    this._cdRef.markForCheck();
  }

  calculationCCurrencyAmount() {
    return +(this.cCurrencyCoinAmount.toNumber() / (this.goldUsdRate / this.ethRate)).toFixed(6)
  }

  /*onBuy() {
    this.progress = true;
    this._cdRef.markForCheck();

    if (this.selectedWallet == 0) {
      this._apiService.goldBuyHwRequest(this.toSpend.toNumber())
        .finally(() => {
          this.progress = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
          const amount = new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18));
          this.confirmation = true;
          this._cdRef.markForCheck();

          this._translate.get('MessageBox.GoldBuy',
            {goldAmount: amount, goldRate: res.data.goldRate}
          ).subscribe(phrase => {
              this.confirmation = false;
              this._messageBox.confirm(phrase).subscribe(ok => {
                if (ok) {
                  this._apiService.confirmHwRequest(true, res.data.requestId).subscribe(() => {
                    this._translate.get('MessageBox.RequestProgress').subscribe(phrase => {
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
      this._apiService.goldBuyRequest(this.ethAddress, this.toSpend.toNumber())
        .finally(() => {
          this.progress = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
          const amount = new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18));
          this.confirmation = true;
          this._cdRef.markForCheck();

          this._translate.get('MessageBox.GoldBuy',
            {goldAmount: amount, goldRate: res.data.goldRate}
          ).subscribe(phrase => {
            this.confirmation = false;
            this._messageBox.confirm(phrase).subscribe(ok => {
              if (ok) {
                this._apiService.confirmMMRequest(true, res.data.requestId).subscribe(() => {
                  this._ethService.sendBuyRequest(this.ethAddress, res.data.payload);
                });
              }
              this._cdRef.markForCheck();
            });
          });
        });
    }
  }*/

  ngOnDestroy() {
    this.destroy$.next(true);
  }

  onCryptoCurrencySubmit() {
    this.loading = true;
    this._apiService.goldBuyAsset(this.ethAddress, this.cCurrencyCoinAmount)
      .finally(() => {
        this.loading = false;
        this._cdRef.markForCheck();
      })
      .subscribe(res => {
        const amount = +(this.cCurrencyCoinAmount / (res.data.goldRate /res.data.ethRate)).toFixed(6);
        this._translate.get('MessageBox.EthDeposit',
          {coinAmount: this.cCurrencyCoinAmount, goldAmount: amount, ethRate: res.data.ethRate}
        ).subscribe(phrase => {
          this._messageBox.confirm(phrase).subscribe(ok => {
            if (ok) {
              this._apiService.goldBuyConfirm(res.data.requestId).subscribe(() => {
                console.log('confirmed');
                // this._ethService.sendBuyRequest(this.ethAddress, res.data.requestId, this.cCurrencyCoinAmount);
              });
            }
          });
        });
      });
  }

}
