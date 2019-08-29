import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  HostBinding,
  OnDestroy,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {User} from "../../interfaces";
import {environment} from "../../../environments/environment";
import {Observable, Subject} from "rxjs";
import {APIService, GoldrateService, MessageBoxService, UserService} from "../../services";
import {CommonService} from "../../services/common.service";
import {Router} from "@angular/router";

@Component({
  selector: 'app-buy-page',
  templateUrl: './buy-page.component.html',
  styleUrls: ['./buy-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'page' }
})
export class BuyPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public loading = true;
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
  public methdosUrl = [
    '/buy/cryptocurrency',
    '/buy/credit-card',
    '/buy/sepa'
  ];

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
    !this.isAuthenticated && (this.loading = false);
    this._commonService.buyAmount = {};

    this.detectLiteWallet();

    this.checkLiteWallet();
    this.checkLiteWalletInterval = setInterval(() => {
      this.checkLiteWallet();
    }, 500);

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
          this.loading = false;
          this._cdRef.markForCheck();
        });

        this.isBlockedCountry && (this.loading = false);
        this._cdRef.markForCheck();
      });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(rate => {
      if (rate) {
        if (!this.rate) {
          this.rate = rate;
          if (+this._commonService.getCookie('fx_deposit_amount')) {
            this.euroAmount =  +this._commonService.getCookie('fx_deposit_amount');
            this.calcAmount(false);
          } else {
            this.goldAmount = 1;
            this.calcAmount(true);
          }
        }
        this.rate = rate;
      }
    });
  }

  detectLiteWallet() {
    if (!window.hasOwnProperty('GoldMint')) {
      this.noMintWallet = true;
      this._cdRef.markForCheck();
    }
  }

  connectLiteWallet() {
    if (this.sumusAddress) return;
    this.noMintWallet ? this.getLiteWalletModal() : this.enableLiteWalletModal();
  }

  getLiteWalletModal() {
    this._userService.showGetLiteWalletModal();
  }

  enableLiteWalletModal() {
    this._userService.showLoginToLiteWalletModal();
  }

  showInvalidNetworkModal() {
    this._userService.showInvalidNetworkModal(environment.isProduction ? 'InvalidNetworkWallet' : 'InvalidNetworkWalletTest', this.allowedWalletNetwork);
  }

  checkLiteWallet() {
    if (window.hasOwnProperty('GoldMint')) {
      this.liteWallet && this.liteWallet.getCurrentNetwork().then(res => {
        if (this.currentWalletNetwork != res) {
          this.currentWalletNetwork = res;
          if (res !== null && res !== this.allowedWalletNetwork) {
            this.showInvalidNetworkModal();
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
    this._commonService.buyAmount.isGold = isGold;
    this._cdRef.markForCheck();
  }

  onMethodSelect(url: string) {
    if (!this.goldAmount || !this.euroAmount) {
      return;
    }

    if(this.noMintWallet) {
      this.getLiteWalletModal()
      return;
    }

    if (!this.sumusAddress) {
      this.enableLiteWalletModal();
      return;
    }

    if (this.isInvalidWalletNetwork) {
      this.showInvalidNetworkModal();
      return;
    }

    if (!this.isAuthenticated) {
      let data = {
        returnToBuyPage: true,
        euroAmount: this.euroAmount,
        goldAmount: this.goldAmount
      }
      this._commonService.setCookie('fx_buy_data', JSON.stringify(data), {'max-age': 21600});
      this.router.navigate(['/signin']);
      return;
    }

    this._commonService.buyAmount.gold = this.goldAmount;
    this._commonService.buyAmount.euro = this.euroAmount;
    this.router.navigate([url]);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    clearInterval(this.checkLiteWalletInterval);
  }

}
