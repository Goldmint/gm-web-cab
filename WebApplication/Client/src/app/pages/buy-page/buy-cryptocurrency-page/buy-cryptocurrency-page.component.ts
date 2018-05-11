import {ChangeDetectorRef, Component, HostBinding, OnInit, ViewChild, ViewEncapsulation} from '@angular/core';
import {TranslateService} from "@ngx-translate/core";
import {APIService, EthereumService, GoldrateService, MessageBoxService, UserService} from "../../../services";
import {Observable} from "rxjs/Observable";
import {TFAInfo} from "../../../interfaces";
import {User} from "../../../interfaces/user";
import {BigNumber} from "bignumber.js";
import {Subject} from "rxjs/Subject";
import {Router} from "@angular/router";
import {Subscription} from "rxjs/Subscription";
import {environment} from "../../../../environments/environment";


@Component({
  selector: 'app-buy-cryptocurrency-page',
  templateUrl: './buy-cryptocurrency-page.component.html',
  styleUrls: ['./buy-cryptocurrency-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class BuyCryptocurrencyPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  @ViewChild('cryptoCurrencyForm') cryptoCurrencyForm;
  @ViewChild('cCurrencyEthAmount') cCurrencyEthAmount;

  public loading = false;
  public isFirstLoad = true;
  public progress = false;
  public locale: string;

  public ethAddress: string = '';
  public selectedWallet = 0;
  public goldRate: number = 0;

  public user: User;
  public tfaInfo: TFAInfo;

  public coinList = ['BTC', 'ETH']
  public currentCoin = this.coinList[1];
  public ethBalance: BigNumber = null;
  public cCurrencyCoinAmount: any = null;
  public cCurrencyAmountView;
  public cCurrencyEstimateAmount;
  public estimateAmountUSD = 0;
  public invalidBalance = false;
  public etherscanUrl = environment.etherscanUrl;
  public sub1: Subscription;

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
    this.loading = true;
    this.cCurrencyEthAmount.valueChanges
      .debounceTime(750)
      .takeUntil(this.destroy$)
      .subscribe(value => {
        if (value) {
          this.onCCurrencyChanged(value.toString());
        } else {
          this.cCurrencyEstimateAmount = 0;
          this.invalidBalance = true;
          this.loading = false;
          this._cdRef.markForCheck();
        }
      });

    Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._userService.currentUser
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1];
        this._cdRef.markForCheck();
      });

    this._userService.currentLocale.takeUntil(this.destroy$).subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(data => {
      data && (this.goldRate = data.gold);
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableEthBalance().takeUntil(this.destroy$).subscribe(balance => {
     if (this.ethBalance === null || !this.ethBalance.eq(balance)) {
        this.ethBalance = balance;
        if (this.ethBalance !== null && this.isFirstLoad) {
          this.setCCurrencyEthBalance();
          this.isFirstLoad = false;
        }
      }
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress) {
        this.selectedWallet = 0;
        this.router.navigate(['buy']);
      }
    });

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;

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

  changeValue() {
    this.loading = true;
  }

  onCCurrencyChanged(value: string) {
    this.invalidBalance = false;
    const testValue = value != null && value.length > 0 ? new BigNumber(value) : new BigNumber(0);
    this.cCurrencyCoinAmount = new BigNumber(0);

    if (testValue.gt(0) && +value <= this.ethBalance.toNumber()) {
      this.cCurrencyCoinAmount = new BigNumber(value);
      this.cCurrencyCoinAmount = this.cCurrencyCoinAmount.decimalPlaces(6, BigNumber.ROUND_DOWN);

      const wei = new BigNumber(this.cCurrencyCoinAmount).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
      this._apiService.goldBuyEstimate(this.currentCoin, wei.toString())
        .finally(() => {
          this.loading = false;
          this._cdRef.markForCheck();
        }).subscribe(data => {
        this.cCurrencyEstimateAmount = data.data.amount / Math.pow(10, 18);
        this.estimateAmountUSD = this.cCurrencyEstimateAmount * this.goldRate;
      });
    } else {
      this.cCurrencyEstimateAmount = this.estimateAmountUSD = 0;
      this.loading = false;
      this.invalidBalance = true;
    }
    this._cdRef.markForCheck();
  }

  setCCurrencyEthBalance(percent: number = 1) {
    this.loading = true;
    const ethBalance = new BigNumber(this.ethBalance.times(percent));
    this.cCurrencyCoinAmount = ethBalance.decimalPlaces(6, BigNumber.ROUND_DOWN);
    this.cCurrencyAmountView = this.cCurrencyCoinAmount;
    this._cdRef.markForCheck();
  }

  onCryptoCurrencySubmit() {
    this.loading = true;
    this.sub1 && this.sub1.unsubscribe();
    this._apiService.goldBuyAsset(this.ethAddress, this.cCurrencyCoinAmount)
      .finally(() => {
        this.loading = false;
        this._cdRef.markForCheck();
      })
      .subscribe(res => {
        const wei = new BigNumber(this.cCurrencyCoinAmount).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
        this._apiService.goldBuyEstimate(this.currentCoin, wei.toString())
          .subscribe(data => {
            const amount = data.data.amount / Math.pow(10, 18);
            this._translate.get('MessageBox.EthDeposit',
              {coinAmount: this.cCurrencyCoinAmount, goldAmount: amount.toFixed(6), ethRate: res.data.ethRate}
            ).subscribe(phrase => {
              this._messageBox.confirm(phrase).subscribe(ok => {
                if (ok) {
                  this._apiService.goldBuyConfirm(res.data.requestId).subscribe(() => {
                    this._ethService.sendBuyRequest(this.ethAddress, this.user.id, res.data.requestId, this.cCurrencyCoinAmount);

                    this.sub1 = this._ethService.getSuccessBuyRequestLink$.subscribe(hash => {
                      if (hash) {
                        this._translate.get('PAGES.Buy.CtyptoCurrency.SuccessModal').subscribe(phrases => {
                          this._messageBox.alert(`
                            <div class="text-center">
                              <div class="font-weight-500 mb-2">${phrases.Heading}</div>
                              <div>${phrases.Steps}</div>
                              <div>${phrases.Hash}</div>
                              <div class="mb-2 buy-hash">${hash}</div>
                              <a href="${this.etherscanUrl}${hash}" target="_blank">${phrases.Link}</a>
                            </div>
                          `);
                        });
                      }
                    });

                  });
                }
              });
            });
        });
      });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
