import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {User} from "../../../interfaces";
import {environment} from "../../../../environments/environment";
import {Observable, Subject} from "rxjs";
import {APIService, GoldrateService, MessageBoxService, UserService} from "../../../services";
import {CommonService} from "../../../services/common.service";
import {NgForm} from "@angular/forms";
import {Router} from "@angular/router";

@Component({
  selector: 'app-buy-credit-card-page',
  templateUrl: './buy-credit-card-page.component.html',
  styleUrls: ['./buy-credit-card-page.component.sass']
})
export class BuyCreditCardPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public isDataLoaded = true;
  public user: User;
  public blockedCountriesList = [];
  public isBlockedCountry: boolean = false;
  public isAuthenticated: boolean = false;
  public euroAmount: number = 0;
  public goldAmount: number = 0;
  public sumusAddress: string = null;
  public noMintWallet: boolean = false;
  public allowedWalletNetwork = environment.walletNetwork;
  public currentWalletNetwork;
  public isInvalidWalletNetwork: boolean = true;
  public creditCard = {
    number: null,
    holder: null,
    expDate: null,
    securityCode: null
  };
  public agreeCheck: boolean = false;
  public loading: boolean = false;
  public isTransactionSent: boolean = false;
  public merchantPageUrl: string = null;
  public tradingLimit: any = null;

  private rate: any = null;
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private liteWallet = null;
  private checkLiteWalletInterval;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _cdRef: ChangeDetectorRef,
    private _commonService: CommonService,
    private _goldrateService: GoldrateService,
    private router: Router
  ) { }

  ngOnInit() {
    this.liteWallet = window['GoldMint'];
    this.isAuthenticated = this._userService.isAuthenticated();

    if (!this.isAuthenticated) {
      this.router.navigate(['/buy']);
      return;
    }

    this.detectLiteWallet();

    this.checkLiteWallet();
    this.checkLiteWalletInterval = setInterval(() => {
      this.checkLiteWallet();
    }, 500);

    this.setDefaultAmount();
    const merchantPageUrl = this._commonService.getCookie('fx_url');
    this.merchantPageUrl = merchantPageUrl ? decodeURIComponent(merchantPageUrl) : null;

    this.isAuthenticated && Observable.combineLatest(
      this._apiService.getProfile(),
      this._apiService.getBannedCountries(),
      this._apiService.getKYCProfile()
    )
      .subscribe((res) => {
        this.user = res[0].data;

        this.blockedCountriesList = res[1] ? res[1].data : [];
        this.isBlockedCountry = this.blockedCountriesList.indexOf(res[2].data['country']) >= 0;
        !this.isBlockedCountry && this._userService.getIPInfo().subscribe(data => {
          this.isBlockedCountry = this.blockedCountriesList.indexOf(data['country']) >= 0;
          this.isDataLoaded = true;
          this._cdRef.markForCheck();
        });

        this.isBlockedCountry && (this.isDataLoaded = true);
        this._cdRef.markForCheck();
      });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(rate => {
      if (rate) {
        if (!this.rate) {
          this.rate = rate;
          this.calcAmount(this._commonService.buyAmount ? this._commonService.buyAmount.isGold : true);
        }
        this.rate = rate;
      }
    });

    this._apiService.transferTradingLimit$.takeUntil(this.destroy$).subscribe((limit: any) => {
      this.tradingLimit = {};
      this.tradingLimit.min = limit.min;
      this.tradingLimit.max = limit.max;
      this._cdRef.markForCheck();
    });
  }

  setDefaultAmount() {
    if (this._commonService.buyAmount) {
      this.goldAmount = this._commonService.buyAmount.gold;
      this.euroAmount = this._commonService.buyAmount.euro;
    } else if (this._commonService.getCookie('fx_buy_data')) {
      try {
        let data = JSON.parse(this._commonService.getCookie('fx_buy_data'));
        this.goldAmount = data.goldAmount;
        this.euroAmount = data.euroAmount;
      } catch (e) {
        this.goldAmount = 1;
      }
    } else if (this._commonService.getCookie('fx_deposit_amount')) {
      this.goldAmount = +this._commonService.getCookie('fx_deposit_amount');
    } else {
      this.goldAmount = 1;
    }
  }

  detectLiteWallet() {
    if (!window.hasOwnProperty('GoldMint')) {
      this.noMintWallet = true;
      this._cdRef.markForCheck();
    }
  }

  getLiteWalletModal() {
    this._userService.showGetLiteWalletModal();
  }

  enableLiteWalletModal() {
    this._userService.showLoginToLiteWalletModal();
  }

  checkLiteWallet() {
    if (window.hasOwnProperty('GoldMint')) {
      this.liteWallet && this.liteWallet.getCurrentNetwork().then(res => {
        if (this.currentWalletNetwork != res) {
          this.currentWalletNetwork = res;
          if (res !== null && res !== this.allowedWalletNetwork) {
            this._userService.showInvalidNetworkModal('InvalidNetworkWallet', this.allowedWalletNetwork);
            this.isInvalidWalletNetwork = true;
          } else {
            this.isInvalidWalletNetwork = false;
          }
          this._cdRef.markForCheck();
        }
      });

      this.liteWallet && this.liteWallet.getAccount().then(res => {
        if (this.sumusAddress != res[0]) {
          this.sumusAddress = res.length ? res[0] : null;
          this._cdRef.markForCheck();
        }
      });
    }
  }

  changeValue(event, isGold: boolean) {
    this.tradingLimit = null;
    event.target.value = this._commonService.substrValue(event.target.value);
    isGold ? (this.goldAmount = +event.target.value) : (this.euroAmount = +event.target.value);
    this.calcAmount(isGold);
  }

  calcAmount(isGold: boolean) {
    if (isGold) {
      this.euroAmount = +this._commonService.substrValue(this.goldAmount * this.rate.eur);
    } else {
      this.goldAmount = +this._commonService.substrValue(this.euroAmount / this.rate.eur);
    }
    this._cdRef.markForCheck();
  }

  validateCardNumber(e) {
    e.target.value = e.target.value.replace(/[^0-9]/g, '');
    this.creditCard.number = e.target.value;
  }

  validateCSecurityCode(e) {
    e.target.value = e.target.value.replace(/[^0-9]/g, '');
    this.creditCard.securityCode = e.target.value;
  }

  onSubmit(form: NgForm) {
    if (form.valid && +this.goldAmount && +this.euroAmount) {
      this.loading = true;
      this._apiService.buyByCreditCard('eur', this.sumusAddress, (this.euroAmount * 100).toString()).subscribe(res => {
        this.isTransactionSent = true;
        this._commonService.deleteFxCookies();
        this.loading = false;
        this._cdRef.markForCheck();
      }, () => {
        this.loading = false;
        this._cdRef.markForCheck();
      });
    }
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    clearInterval(this.checkLiteWalletInterval);
  }

}
