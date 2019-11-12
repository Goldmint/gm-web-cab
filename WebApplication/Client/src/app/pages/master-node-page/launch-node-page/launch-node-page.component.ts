import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {UserService} from "../../../services/user.service";
import {Subscription} from "rxjs/Subscription";
import {environment} from "../../../../environments/environment";
import {User} from "../../../interfaces";
import {Subject} from "rxjs";
import {combineLatest} from "rxjs/observable/combineLatest";
import {APIService, EthereumService, MessageBoxService} from "../../../services";
import {TranslateService} from "@ngx-translate/core";
import {PoolService} from "../../../services/pool.service";
import * as Web3 from "web3";
import * as bs58 from 'bs58';

@Component({
  selector: 'app-launch-node-page',
  templateUrl: './launch-node-page.component.html',
  styleUrls: ['./launch-node-page.component.sass']
})
export class LaunchNodePageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public sumusAddress: string = null;
  public ethAddress: string | null;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isStakeDataLoaded: boolean = false;
  public isMetamask: boolean = false;
  public tokenAmount: number = 0;
  public etherscanUrl = environment.etherscanUrl;
  public agreeCheck: boolean = false;
  public allowedMMNetwork = environment.MMNetwork;
  public allowedWalletNetwork = environment.walletNetwork;
  public currentWalletNetwork;
  public isInvalidMMNetwork: boolean = true;
  public isInvalidWalletNetwork: boolean = true;
  public userStake: number;
  public userFrozenStake: number;
  public errors = {
    haveToHold: false,
    stakeSent: false
  };
  public noMetamask: boolean = false;
  public noMintWallet: boolean = false;
  public liteWalletLink;

  private destroy$: Subject<boolean> = new Subject<boolean>();
  private liteWallet = null;
  private checkLiteWalletInterval;
  private sub1: Subscription;
  private Web3 = new Web3();

  public locale: string;
  public isSent: boolean = false;

  constructor(
    private userService: UserService,
    private messageBox: MessageBoxService,
    private apiService: APIService,
    private ethService: EthereumService,
    private _cdRef: ChangeDetectorRef,
    private translate: TranslateService,
    private poolService: PoolService
  ) { }

  ngOnInit() {
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

    const combined = combineLatest(
      this.poolService.getObsUserStake(),
      this.poolService.getObsUserFrozenStake()
    );

    combined.takeUntil(this.destroy$).subscribe((data: any) => {
      if (data[0] !== null && data[1] !== null) {
        this.userStake = +data[0];
        this.userFrozenStake = +data[1];

        if ((this.userFrozenStake == 0 && this.userStake >= 10000) || this.userFrozenStake > 0) {
          if (this.userStake - this.userFrozenStake == 0) {
            this.errors.stakeSent = true;
          } else {
            this.tokenAmount = this.userStake - this.userFrozenStake;
          }
        } else {
          this.errors.haveToHold = true;
        }
        this.isStakeDataLoaded = true;
        this._cdRef.markForCheck();
      }
    });

    this.userService.currentLocale.takeUntil(this.destroy$).subscribe(locale => {
      this.locale = locale;
    });
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
    this.userService.showLoginToMMBox('MasterNode');
  }

  getMintWalletModal() {
    this.userService.showGetLiteWalletModal();
  }

  enableMintWalletModal() {
    this.userService.showLoginToLiteWalletModal();
  }

  initModal() {
    this.poolService.getSuccessMNTTokenLink$.takeUntil(this.destroy$).subscribe((hash: string) => {
      if (hash) {
        this.successModal(hash);
        this.isSent = true;
        this.tokenAmount = 0;
        this._cdRef.markForCheck();
      }
    });
  }

  successModal(hash: string) {
    this.translate.get('PAGES.Sell.CtyptoCurrency.SuccessModal').subscribe(phrases => {
      this.messageBox.alert(`
        <div class="text-center">
          <div class="font-weight-500 mb-2">${phrases.Heading}</div>
          <div class="color-red">${phrases.Steps}</div>
          <div>${phrases.Hash}</div>
          <div class="mb-2 sell-hash">${hash}</div>
          <a href="${this.etherscanUrl}${hash}" target="_blank">${phrases.Link}</a>
        </div>
        `).subscribe();
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

        this.poolService.freezeStake(bytes32, this.ethAddress, +price * Math.pow(10, 9));
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
