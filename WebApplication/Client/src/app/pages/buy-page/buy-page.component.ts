import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, OnDestroy,
  ViewChild
} from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import { GoldBuyResponse } from '../../interfaces'
import { Subscription } from 'rxjs/Subscription';
import { Observable } from "rxjs/Observable";
import { BigNumber } from 'bignumber.js'
import {Router} from "@angular/router";
import {TranslateService} from "@ngx-translate/core";
import {Subject} from "rxjs/Subject";

enum Pages { CryptoCurrency,  CryptoCapital }

@Component({
  selector: 'app-buy-page',
  templateUrl: './buy-page.component.html',
  styleUrls: ['./buy-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'page' }
})
export class BuyPageComponent implements OnInit, OnDestroy {

  @ViewChild('inputToSpend') inputToSpend;

  confirmation: boolean = false;
  progress: boolean = false;
  toSpendUnset: boolean = true;
  toSpend: BigNumber = new BigNumber(0);

  usdBalance: number = 0;
  goldUsdRate: number = 0;
  estimatedAmount: string = "";
  public buyAmountChecked: boolean = true;
  public ethAddress: string = '';
  public selectedWallet = 0;

  // ----------

  public pages = Pages;
  public page: Pages;

  public loading = false;
  public isBalancesLoaded = false;
  public coinList = ['btc', 'eth']
  public currentCoin = this.coinList[1];
  public ethRate: number;
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
    private router: Router,
    private _translate: TranslateService
  ) { }

  ngOnInit() {
    this.page = Pages.CryptoCurrency;

    this._apiService.getEthereumRate().subscribe((data) => {
        this.ethRate = data.data.usd;
        this.setCCurrencyEthBalance();
      });


    this._ethService.getObservableEthBalance().takeUntil(this.destroy$).subscribe(balance => {
      if (this.ethBalance === null || !this.ethBalance.eq(balance)) {
        this.ethBalance = balance;
        this.setCCurrencyEthBalance();
      }
    });

    this._goldrateService.getObservableRate().subscribe(rate => {
      if (rate > 0 && rate !== this.goldUsdRate) {
        this.goldUsdRate = rate;
        if (this.cCurrencyCoinAmount !== null) {
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
      // this.isBalancesLoaded = false;
      this.setCCurrencyEthBalance();
    });

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;
    // this.setCCurrencyEthBalance();

    this._userService.onWalletSwitch$.takeUntil(this.destroy$).subscribe((wallet) => {
      this.selectedWallet = wallet['id'] === 'hot' ? 0 : 1;
      this.setCCurrencyEthBalance();
    });

  }

  onCCurrencyChanged(value: string) {
    const testValue = value != null && value.length > 0 ? new BigNumber(value) : new BigNumber(0);
    this.cCurrencyCoinAmount = new BigNumber(0);

    if (testValue.gt(0)) {
      this.cCurrencyCoinAmount = new BigNumber(value);
      this.cCurrencyCoinAmount = this.cCurrencyCoinAmount.decimalPlaces(6, BigNumber.ROUND_DOWN);
    }
    this.cCurrencyEstimateAmount = this.calculationCCurrencyAmount();
    this._cdRef.markForCheck();
  }

  setCCurrencyEthBalance(percent: number = 1) {
    let ethBalance = this.selectedWallet == 0 ? this.hotEthBalance : this.ethBalance;
    if (!ethBalance) {
      return;
    }
    ethBalance = new BigNumber(ethBalance.times(percent));
    // got gold balance first time
    this.isBalancesLoaded = true;
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

  /*buyAmountCheck(val: BigNumber) {
    this.buyAmountChecked = val.gte(1) && this.usdBalance && val.lte(this.usdBalance);
  }*/

  ngOnDestroy() {
    this.destroy$.next(true);
  }

  // ------------

  chooseCurrentCoin(coin) {
    if (this.currentCoin !== coin) {
      this.currentCoin = coin;
    }
  }

  /*onCryptoCurrencyChanged(value) {
    if (value != null && value > 0) {
      this.cCurrencyCoinAmount = new BigNumber(value);
      this.cCurrencyCoinAmount = this.cCurrencyCoinAmount.decimalPlaces(6, BigNumber.ROUND_DOWN);
      this.cCurrencyEstimateAmount = value / (this.goldUsdRate / this.ethRate);
      // this.cCurrencyEstimateAmount = value * this.ethRate;
    } else {
      this.cCurrencyEstimateAmount = 0;
    }
  }*/

  onCryptoCurrencySubmit() {
    this.loading = true;
    this._apiService.ethDepositRequest(this.ethAddress, this.cCurrencyCoinAmount)
      .finally(() => {
        this.loading = false;
        this._cdRef.markForCheck();
      })
      .subscribe(res => {
        const amount = (this.cCurrencyCoinAmount * res.data.ethRate).toFixed(2);
        this._translate.get('MessageBox.EthDeposit',
          {coinAmount: this.cCurrencyCoinAmount, usdAmount: amount, ethRate: res.data.ethRate}
        ).subscribe(phrase => {
          this._messageBox.confirm(phrase).subscribe(ok => {
            if (ok) {
              this._apiService.confirmEthDepositRequest(true, res.data.requestId).subscribe(() => {
                this._ethService.ethDepositRequest(this.ethAddress, res.data.requestId, this.cCurrencyCoinAmount);
              });
            }
          });
        });
      });
  }

}
