import {ChangeDetectionStrategy, ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {BigNumber} from "bignumber.js";
import {CommonService} from "../../../services/common.service";
import {EthereumService, MessageBoxService, UserService} from "../../../services";
import {Subject} from "rxjs/Subject";
import {Subscription} from "rxjs/Subscription";
import {environment} from "../../../../environments/environment";
import {PoolService} from "../../../services/pool.service";
import {TranslateService} from "@ngx-translate/core";
import * as Web3 from "web3";

@Component({
  selector: 'app-hold-tokens-page',
  templateUrl: './hold-tokens-page.component.html',
  styleUrls: ['./hold-tokens-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HoldTokensPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public isProduction = environment.isProduction;
  public loading: boolean = false;
  public tokenBalance: BigNumber | null = null;
  public ethAddress: string = '';
  public tokenAmount: number = 0;
  public etherscanUrl = environment.etherscanUrl;
  public interval: Subscription;
  public MMNetwork = environment.MMNetwork;

  public invalidBalance: boolean = false;
  public isAuthenticated: boolean = false;
  public isInvalidNetwork: boolean = true;

  private Web3 = new Web3();
  private timeoutPopUp;
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private sub1: Subscription;

  constructor(
    private _commonService: CommonService,
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private _ethService: EthereumService,
    private _messageBox: MessageBoxService,
    private _poolService: PoolService,
    private _translate: TranslateService
  ) { }

  ngOnInit() {
    this.isAuthenticated = this._userService.isAuthenticated();
    this.initSuccessTransactionModal();

    if (window.hasOwnProperty('web3') || window.hasOwnProperty('ethereum')) {
      this.loading = true;
      this.timeoutPopUp = setTimeout(() => {
        !this.ethAddress && this._userService.showLoginToMMBox('HeadingPool');
        this.loading = false;
        this._cdRef.markForCheck();
      }, 4000);
    }

    this._ethService.getObservableMntpBalance().takeUntil(this.destroy$).subscribe(balance => {
      if (balance !== null && (this.tokenBalance === null || !this.tokenBalance.eq(balance))) {
        this.tokenBalance = balance;
        this.setCoinBalance(1);
        this.loading = false;
        this._cdRef.markForCheck();
      }
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress && this.tokenBalance !== null) {
        this.tokenBalance = null;
        this.tokenAmount = 0;
      }
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableNetwork().takeUntil(this.destroy$).subscribe(network => {
      if (network !== null) {
        if (network != this.MMNetwork.index) {
          this._userService.invalidNetworkModal(this.MMNetwork.name);
          this.isInvalidNetwork = true;
        } else {
          this.isInvalidNetwork = false;
        }
        this._cdRef.markForCheck();
      }
    });

  }

  initSuccessTransactionModal() {
    this._poolService.getSuccessHoldRequestLink$.takeUntil(this.destroy$).subscribe(hash => {
      if (hash) {
        this._translate.get('MessageBox.SuccessTransactionModal').subscribe(phrases => {
          this._poolService.successTransactionModal(hash, phrases);
        });
      }
    });
  }

  changeValue(event) {
    event.target.value = this._commonService.substrValue(event.target.value);
    this.tokenAmount = +event.target.value;
    event.target.setSelectionRange(event.target.value.length, event.target.value.length);
    this.checkEnteredAmount();
    this._cdRef.markForCheck();
  }

  setCoinBalance(percent) {
    const value = this._commonService.substrValue(+this.tokenBalance * percent);
    this.tokenAmount = +value;
    this.checkEnteredAmount();
    this._cdRef.markForCheck();
  }

  checkEnteredAmount() {
    this.invalidBalance = this.tokenAmount > +this.tokenBalance;
  }

  onSubmit() {
    if (!this.isAuthenticated) {
      this._messageBox.authModal();
      return;
    }

    let firstLoad = true;
    this.sub1 && this.sub1.unsubscribe();
    this.sub1 = this._ethService.getObservableGasPrice().takeUntil(this.destroy$).subscribe((price) => {
      if (price && firstLoad) {
        firstLoad = false;
        const wei = this.Web3.toWei(this.tokenAmount);
        this._poolService.holdStake(this.ethAddress, wei, +price * Math.pow(10, 9));
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    clearTimeout(this.timeoutPopUp);
  }

}
