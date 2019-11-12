import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {environment} from "../../../environments/environment";
import {User} from "../../interfaces";
import {Subject, Subscription} from "rxjs";
import {APIService, EthereumService, MessageBoxService, UserService} from "../../services";
import {TranslateService} from "@ngx-translate/core";
import {PoolService} from "../../services/pool.service";
import * as bs58 from 'bs58';
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
  public tokenAmount: number = 0;
  public etherscanUrl = environment.etherscanUrl;
  public etherscanContractUrl = environment.etherscanContractUrl;
  public user: User;
  public isAuthenticated: boolean = false;
  public agreeCheck: boolean = false;
  public allowedMMNetwork = environment.MMNetwork;
  public allowedWalletNetwork = environment.walletNetwork;
  public currentWalletNetwork;
  public isInvalidMMNetwork: boolean = true;
  public isInvalidWalletNetwork: boolean = true;
  public errors = {
    invalidMntpValue: false
  };
  public noMetamask: boolean = false;
  public noMintWallet: boolean = false;
  public liteWalletLink;
  public locale: string;
  public swapMntpTxHash: string = null;
  public swapContractAddress = environment.SwapContractAddress;

  private destroy$: Subject<boolean> = new Subject<boolean>();
  private liteWallet = null;
  private checkLiteWalletInterval;
  private sub1: Subscription;
  private mntpBalance: number = 0;

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

    this.liteWallet = window['GoldMint'];
    let isFirefox = typeof window['InstallTrigger'] !== 'undefined';
    this.liteWalletLink = isFirefox ? environment.getLiteWalletLink.firefox : environment.getLiteWalletLink.chrome;

    this.initModal();
    this.getEthAddress();
    this.detectMetaMask();
    this.detectLiteWallet();

    this.isDataLoaded = true;
    this._cdRef.markForCheck();

    this.checkLiteWallet();
    this.checkLiteWalletInterval = setInterval(() => {
      this.checkLiteWallet();
    }, 500);

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

    // this.apiService.getProfile().subscribe(data => {
    //   this.user = data.data;
    //   this._cdRef.markForCheck();
    // });

    this.ethService.getObservableMntpBalance().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null && +data !== this.mntpBalance) {
        this.mntpBalance = +data;
        this.tokenAmount = +this.commonService.substrValue(this.mntpBalance);
        this._cdRef.markForCheck();
      }
    });

    this.userService.currentLocale.takeUntil(this.destroy$).subscribe(locale => {
      this.locale = locale;
    });
  }

  changeValue(event) {
    event.target.value = this.commonService.substrValue(event.target.value);
    this.tokenAmount = +event.target.value;
    this.errors.invalidMntpValue = this.tokenAmount > this.mntpBalance;
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
        this.tokenAmount = 0;
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
    if (!window.hasOwnProperty('GoldMint')) {
      this.noMintWallet = true;
      this._cdRef.markForCheck();
    }
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
    if (window.hasOwnProperty('GoldMint')) {
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
          this.sumusAddress = res.length ? res[0] : null;
          this._cdRef.markForCheck();
        }
      });
    }
  }

  toHexString(byteArray) {
    var s = '0x';
    byteArray.forEach(function(byte) {
      s += ('0' + (byte & 0xFF).toString(16)).slice(-2);
    });
    return s;
  }

  onSubmit() {
    let firstLoad = true;
    this.sub1 && this.sub1.unsubscribe();
    this.sub1 = this.ethService.getObservableGasPrice().takeUntil(this.destroy$).subscribe((price) => {
      if (price && firstLoad) {
        firstLoad = false;
        let bytes = bs58.decode(this.sumusAddress),
          byteArray = bytes.slice(0, -4),
          bytes32 = this.toHexString(byteArray);

        this.ethService.swapMNTP(bytes32, this.ethAddress, this.tokenAmount,+price * Math.pow(10, 9));
      }
    });
    this._cdRef.markForCheck();
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    clearInterval(this.checkLiteWalletInterval);
    this.sub1 && this.sub1.unsubscribe();
  }

}
