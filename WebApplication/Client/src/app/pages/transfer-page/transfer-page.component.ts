import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, HostBinding, ChangeDetectorRef,
  OnDestroy
} from '@angular/core';
import { BsModalService } from 'ngx-bootstrap/modal';
import { MessageBoxService, EthereumService } from "../../services/index";
import { BigNumber } from 'bignumber.js'
import {Observable} from "rxjs/Observable";
import {APIService, UserService} from "../../services";
import {Subscription} from "rxjs/Subscription";
import {Router} from "@angular/router";
import {Subject} from "rxjs/Subject";
import {TranslateService} from "@ngx-translate/core";
import {TFAInfo} from "../../interfaces";
import * as Web3 from "web3";
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-transfer-page',
  templateUrl: './transfer-page.component.html',
  styleUrls: ['./transfer-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransferPageComponent implements OnInit, OnDestroy {
  @HostBinding('class') class = 'page';

  public amount: BigNumber = new BigNumber(0);
  public walletAddress: string = null;
  public goldBalance: BigNumber = null;
  public walletChecked: boolean = false;
  public coincidesAddress: boolean = false;
  public isFirstLoad: boolean = true;
  public isFirstTransaction = true;
  public loading: boolean = true;
  public invalidAmount: boolean = false;
  public showConfirmBlock: boolean = false;
  public user;
  public tfaInfo: TFAInfo;
  public isMetamask: boolean = false;

  public amountValue: number;
  public ethAddress: string = '';
  public selectedWallet = 0;
  public MMNetwork = environment.MMNetwork;
  public isInvalidNetwork: boolean = true;
  public isAuthenticated: boolean = false;

  public etherscanUrl = environment.etherscanUrl;
  private sub1: Subscription;
  public subGetGas: Subscription;
  private timeoutPopUp;
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private Web3 = new Web3();

  constructor(
    private _modalService: BsModalService,
    private _ethService: EthereumService,
    private _messageBox: MessageBoxService,
    private _apiService: APIService,
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private router: Router,
    private _translate: TranslateService
  ) { }

  ngOnInit() {
    this.isAuthenticated = this._userService.isAuthenticated();
    !this.isAuthenticated && (this.loading = false);

    this.isAuthenticated && Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._apiService.getProfile()
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1].data;
        this.loading = false;

        if (!window.hasOwnProperty('web3') && !window.hasOwnProperty('ethereum') && this.user.verifiedL1) {
          this._translate.get('MessageBox.MetaMask').subscribe(phrase => {
            this._messageBox.alert(phrase.Text, phrase.Heading);
          });
        }

        this._cdRef.markForCheck();
      });

    if (window.hasOwnProperty('web3') || window.hasOwnProperty('ethereum')) {
      this.timeoutPopUp = setTimeout(() => {
        !this.isMetamask && this._userService.showLoginToMMBox('HeadingTransfer')
      }, 3000);
    }

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (this.ethAddress !== null) {
        this.isMetamask = true;
        this.onWalletAddressChanged(this.walletAddress);
      }

      if (!this.ethAddress && this.goldBalance !== null) {
        this.isMetamask = false;
        this.amountValue = 0;
        this.goldBalance = new BigNumber(0);
      }
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableGoldBalance().takeUntil(this.destroy$).subscribe(data => {
      if ( data !== null && (this.goldBalance === null || !this.goldBalance.eq(data)) ) {
        this.goldBalance = data;

        if (this.isFirstLoad) {
          this.isFirstLoad = false;
          this.setGoldBalance();
        }
        this._cdRef.markForCheck();
      }
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

  setGoldBalance(percent: number = 1) {
    const goldAmount = +this.goldBalance.decimalPlaces(6, BigNumber.ROUND_DOWN) * percent;
    this.amountValue = +this.substrValue(goldAmount);
    this.invalidAmount = this.amountValue > 0 ? false : true;
  }

  onAmountChanged(event) {
    event.target.value = this.substrValue(event.target.value);
    event.target.setSelectionRange(event.target.value.length, event.target.value.length);

    if (event.target.value > +this.goldBalance || +!this.goldBalance || event.target.value <= 0) {
      this.invalidAmount = true;
    } else {
      this.invalidAmount = false;
    }
  }

  substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d+)(?:(\.\d{0,6})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  onWalletAddressChanged(value: string) {
    this.walletChecked = this.coincidesAddress = false;

    if (this._ethService.isValidAddress(value)) {
      if (value.toLowerCase() === this.ethAddress.toLowerCase()) {
        this.coincidesAddress = true;
      }
      this.walletAddress = value;
      this.walletChecked = true;
    }
    this._cdRef.markForCheck();
  }

  onSubmit() {
    if (!this.isAuthenticated) {
      this._messageBox.authModal();
      return;
    }

    this.sub1 && this.sub1.unsubscribe();
    this.subGetGas && this.subGetGas.unsubscribe();
    this.isFirstTransaction = true;

    const ToAddress = this.walletAddress.slice(0, 6) + '****' + this.walletAddress.slice(-4);
    const FromAddress = this.ethAddress.slice(0, 6) + '****' + this.ethAddress.slice(-4);
    const amount = this.Web3.toWei(this.amountValue);

    this._translate.get('MessageBox.GoldTransfer',
      {ToAddress: ToAddress, FromAddress: FromAddress, amount: this.amountValue}
    ).subscribe(phrase => {
      this._messageBox.confirm(phrase).subscribe(ok => {
        if (ok) {
          this.subGetGas = this._ethService.getObservableGasPrice().subscribe((price) => {
            if (price !== null && this.isFirstTransaction) {
              this.showConfirmBlock = true;
              this._ethService.transferGoldToWallet(this.ethAddress, this.walletAddress, amount, +price * Math.pow(10, 9));
              this.isFirstTransaction = false;
              this._cdRef.markForCheck();
            }
          });

          this.sub1 = this._ethService.getSuccessSellRequestLink$.subscribe(hash => {
            if (hash) {
              this.showConfirmBlock = false;
              this._translate.get('PAGES.Sell.CtyptoCurrency.SuccessModal').subscribe(phrases => {
                this._messageBox.alert(`
                  <div class="text-center">
                    <div class="font-weight-500 mb-2">${phrases.Heading}</div>
                    <div class="color-red">${phrases.Steps}</div>
                    <div>${phrases.Hash}</div>
                    <div class="mb-2 sell-hash">${hash}</div>
                    <a href="${this.etherscanUrl}${hash}" target="_blank">${phrases.Link}</a>
                  </div>
                `);
              });
              this._cdRef.markForCheck();
            }
          });
        }
      })
    });
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
    this.subGetGas && this.subGetGas.unsubscribe();
    this.destroy$.next(true);
    clearTimeout(this.timeoutPopUp);
  }

}
