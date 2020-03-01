import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {environment} from "../../../environments/environment";
import {Subject, Subscription} from "rxjs";
import {APIService, EthereumService, MessageBoxService, UserService} from "../../services";
import {TranslateService} from "@ngx-translate/core";
import {PoolService} from "../../services/pool.service";
import * as bs58 from 'bs58';
import { BigNumber } from 'bignumber.js';
import {CommonService} from "../../services/common.service";

@Component({
  selector: 'app-swap-mntp',
  templateUrl: './swap-mntp.component.html',
  styleUrls: ['./swap-mntp.component.sass']
})
export class SwapMntpComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public switchModel: any = {};
  public sumusAddress: string = null;
  public ethAddress: string | null;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isMetamask: boolean = false;
  public mntpAmount: number = 0;
  public mintAmount: number | string = 0;
  public etherscanUrl = environment.etherscanUrl;
  public etherscanContractUrl = environment.etherscanContractUrl;
  public agreeMntpConditions: boolean = false;
  public agreeMintConditions: boolean = false;
  public allowedMMNetwork = environment.MMNetwork;
  public allowedWalletNetwork = environment.walletNetwork;
  public currentWalletNetwork;
  public isInvalidMMNetwork: boolean = true;
  public isInvalidWalletNetwork: boolean = true;
  public errors = {
    mntpAmount: false,
    mintAmount: false
  };
  public noMetamask: boolean = false;
  public noMintWallet: boolean = false;
  public liteWalletLink;
  public swapMntpTxHash: string = null;
  public swapMintTxHash: string = null;
  public swapContractAddress = environment.SwapContractAddress;
  private mntFee: number = 0.02;

  private destroy$: Subject<boolean> = new Subject<boolean>();
  private liteWallet = null;
  private checkLiteWalletInterval;
  private checkLiteWalletBalanceInterval;
  private sub1: Subscription;
  private mntpBalance: number = 0;
  private mintBalance: BigNumber = new BigNumber(0);

  constructor(
    private userService: UserService,
    private messageBox: MessageBoxService,
    private apiService: APIService,
    private ethService: EthereumService,
    private _cdRef: ChangeDetectorRef,
    private translate: TranslateService,
    private poolService: PoolService,
    private commonService: CommonService
  ) { }

  ngOnInit() {
    this.switchModel.type = 'mntp';

    let isFirefox = typeof window['InstallTrigger'] !== 'undefined';
    this.liteWalletLink = isFirefox ? environment.getLiteWalletLink.firefox : environment.getLiteWalletLink.chrome;

    this.initModal();
    this.getEthAddress();
    this.detectMetaMask();

    this.checkLiteWallet();
    this.checkLiteWalletInterval = setInterval(() => {
      this.checkLiteWallet();
    }, 500);

    this.isDataLoaded = true;
    this._cdRef.markForCheck();

    this.ethService.getObservableNetwork().takeUntil(this.destroy$).subscribe(network => {
      if (network !== null) {
        if (network != this.allowedMMNetwork.index) {
          this.userService.showInvalidNetworkModal('InvalidNetworkMM', this.allowedMMNetwork.name);
          this.isInvalidMMNetwork = true;
        } else {
          this.isInvalidMMNetwork = false;
        }
        this._cdRef.markForCheck();
      }
    });

    this.ethService.getObservableMntpBalance().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null && +data !== this.mntpBalance) {
        this.mntpBalance = +data;
        this.mntpAmount = +this.commonService.substrValue(this.mntpBalance);
        this._cdRef.markForCheck();
      }
    });
  }

  changeValue(event, token: string) {
    event.target.value = this.commonService.substrValue(event.target.value);
    this[token + 'Amount'] = +event.target.value;
    this.errors[token + 'Amount'] = this[token + 'Amount'] > this[token + 'Balance'];
    this._cdRef.markForCheck();
  }

  setTokenAmount(percent: number, token: string) {
    const value = this.commonService.substrValue(+this[token + 'Balance'] * percent);
    this[token + 'Amount'] = +value;
    this.errors[token + 'Amount'] = this[token + 'Amount'] > this[token + 'Balance'];
    this._cdRef.markForCheck();
  }

  getEthAddress() {
    this.ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(address => {
      if (!this.ethAddress && address) {
        this.messageBox.closeModal();
      }
      this.ethAddress = address;
      if (this.ethAddress !== null) {
        this.isMetamask = true;
      }
      if (!this.ethAddress && this.isMetamask) {
        this.isMetamask = false;
        this.mntpAmount = 0;
        this.mntpBalance = 0;
      }
      this._cdRef.markForCheck();
    });

  }

  detectMetaMask() {
    if (!window['ethereum'] || !window['ethereum'].isMetaMask) {
      this.noMetamask = true;
      this._cdRef.markForCheck();
    }
  }

  detectLiteWallet() {
    this.noMintWallet = !window.hasOwnProperty('GoldMint');
    this._cdRef.markForCheck();
  }

  getMetamaskModal() {
    this.userService.showGetMetamaskModal();
  }

  enableMetamaskModal() {
    this.ethService.connectToMetaMask();
    this.userService.showLoginToMMBox('SwapMNTP');
  }

  getMintWalletModal() {
    this.userService.showGetLiteWalletModal();
  }

  enableMintWalletModal() {
    this.userService.showLoginToLiteWalletModal();
  }

  initModal() {
    this.ethService.getSuccessSwapMNTPLink$.takeUntil(this.destroy$).subscribe((hash: string) => {
      if (hash) {
        this.swapMntpTxHash = hash;
        this._cdRef.markForCheck();
      }
    });
  }

  checkLiteWallet() {
    !this.liteWallet && this.detectLiteWallet();

    if (window.hasOwnProperty('GoldMint')) {
      this.liteWallet = window['GoldMint'];

      this.liteWallet && this.liteWallet.getCurrentNetwork().then(res => {
        if (this.currentWalletNetwork != res) {
          this.currentWalletNetwork = res;
          if (res !== null && res !== this.allowedWalletNetwork) {
            this.userService.showInvalidNetworkModal('InvalidNetworkWallet', this.allowedWalletNetwork);
            this.isInvalidWalletNetwork = true;
          } else {
            this.isInvalidWalletNetwork = false;
          }
          this._cdRef.markForCheck();
        }
      });

      this.liteWallet && this.liteWallet.getAccount().then(res => {
        if (this.sumusAddress != res[0]) {
          clearInterval(this.checkLiteWalletBalanceInterval);
          this.sumusAddress = res.length ? res[0] : null;
          if (this.sumusAddress) {
            this.checkLiteWalletBalance();
            this.checkLiteWalletBalanceInterval = setInterval(() => {
              this.checkLiteWalletBalance();
            }, 7500);
          } else {
            this.mintBalance = new BigNumber(0);
          }
          this._cdRef.markForCheck();
        }
      });
    }
  }

  private checkLiteWalletBalance() {
    this.liteWallet.getBalance(this.sumusAddress).then(res => {
      if (res) {
        const balance = (+res.mint - this.mntFee) > 0 ? new BigNumber(res.mint).minus(this.mntFee) : new BigNumber(0);
        if (!this.mintBalance.isEqualTo(balance)) {
          this.mintBalance = balance;
          this.setMintAmount(1);
          this._cdRef.markForCheck();
        }
      }
    });
  }

  setMintAmount(percent: number) {
    const value = this.mintBalance.multipliedBy(percent);
    const valueStr = this.substrValue(value.toString(10));

    this.mintAmount = new BigNumber(valueStr).toString(10);
    this.errors.mintAmount = this.mintBalance.isLessThan(new BigNumber(valueStr));
    this._cdRef.markForCheck();
  }

  changeMintAmount(event) {
    const value = this.substrValue(event.target.value);
    event.target.value = value;

    this.mintAmount = value;
    this.errors.mintAmount = this.mintBalance.isLessThan(new BigNumber(value || 0));
    this._cdRef.markForCheck();
  }

  private substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d{1,6})\d*(?:(\.\d{0,18})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  toHexString(byteArray) {
    var s = '0x';
    byteArray.forEach(function(byte) {
      s += ('0' + (byte & 0xFF).toString(16)).slice(-2);
    });
    return s;
  }

  swapMNTP() {
    let firstLoad = true;
    this.sub1 && this.sub1.unsubscribe();
    this.sub1 = this.ethService.getObservableGasPrice().takeUntil(this.destroy$).subscribe((price) => {
      if (price && firstLoad) {
        firstLoad = false;
        let bytes = bs58.decode(this.sumusAddress),
          byteArray = bytes.slice(0, -4),
          bytes32 = this.toHexString(byteArray);

        this.ethService.swapMNTP(bytes32, this.ethAddress, this.mntpAmount,+price * Math.pow(10, 9));
      }
    });
    this._cdRef.markForCheck();
  }

  swapMNT() {
    let message: any = {
      src: this.sumusAddress,
      dst: this.ethAddress,
      stamp: Math.round(new Date().getTime() / 1000)
    };
    message = JSON.stringify(message);

    const enc = new window['TextEncoder']();
    const messageUint8Array = enc.encode(message)

    this.liteWallet.signMessage(messageUint8Array).then(res => {
        if (res) {
          const model = {
            mint: this.sumusAddress,
            mint_msg: message,
            mint_sig: res,
            eth: this.ethAddress
          }

          this.loading = true;
          this.apiService.swapMNT(model).subscribe((data: any) => {
            if (data && data.data) {
              const amount = new BigNumber(this.mintAmount).toString(10);
              this.liteWallet.sendTransaction(data.data.swap_address, 'MNT', amount).then(digest => {
                if (digest) {
                  this.swapMintTxHash = digest;
                  this._cdRef.markForCheck();
                }
              });
            }
            this.loading = false;
            this._cdRef.markForCheck();
          }, () => {
            this.loading = false;
            this._cdRef.markForCheck();
          });
        }
    });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    clearInterval(this.checkLiteWalletInterval);
    clearInterval(this.checkLiteWalletBalanceInterval);
    this.sub1 && this.sub1.unsubscribe();
  }

}
