import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {User} from "../../../interfaces";
import {Observable, Subject} from "rxjs";
import {APIService, GoldrateService, MessageBoxService, UserService} from "../../../services";
import {CommonService} from "../../../services/common.service";
import {NgForm} from "@angular/forms";
import {Router} from "@angular/router";
import * as IBAN from "iban"
import {environment} from "../../../../environments/environment";

@Component({
  selector: 'app-buy-sepa-page',
  templateUrl: './buy-sepa-page.component.html',
  styleUrls: ['./buy-sepa-page.component.sass']
})
export class BuySepaPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public isDataLoaded = true;
  public user: User;
  public blockedCountriesList = [];
  public isBlockedCountry: boolean = false;
  public isAuthenticated: boolean = false;
  public sumusAddress: string = null;
  public noMintWallet: boolean = false;
  public allowedWalletNetwork = environment.walletNetwork;
  public currentWalletNetwork;
  public isInvalidWalletNetwork: boolean = true;
  public payerData = {
    name: null,
    holderName: null,
    iban: null
  };
  public agreeCheck: boolean = false;
  public loading: boolean = false;
  public isTransactionSent: boolean = false;
  public currentDate = new Date();
  public isIBANValid: boolean = false;
  public noKycVerification: boolean = false;

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

    this.isAuthenticated && Observable.combineLatest(
      this._apiService.getProfile(),
      this._apiService.getBannedCountries(),
      this._apiService.getKYCProfile()
    )
      .subscribe((res) => {
        this.user = res[0].data;

        this.payerData.name = this.user.name;
        this.payerData.holderName = this.user.name;

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
            this._userService.showInvalidNetworkModal(environment.isProduction ? 'InvalidNetworkWallet' : 'InvalidNetworkWalletTest', this.allowedWalletNetwork);
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

  validateIBAN(e) {
    this.isIBANValid = IBAN.isValid(e.target.value);
    this._cdRef.markForCheck();
  }

  onSubmit(form: NgForm) {
    if (form.valid) {
      this.loading = true;
      this._apiService.buyByCreditCard('eur', this.sumusAddress, (100 * 100).toString()).subscribe(res => {
        if (!this.user.verifiedL1) {
          this.noKycVerification = true;
        } else {
          this.isTransactionSent = true;
        }

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
