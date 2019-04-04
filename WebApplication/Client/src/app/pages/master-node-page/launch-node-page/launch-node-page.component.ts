import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import { DeviceDetectorService } from 'ngx-device-detector';
import {UserService} from "../../../services/user.service";
import {Subscription} from "rxjs/Subscription";
import {DomSanitizer} from "@angular/platform-browser";
import {environment} from "../../../../environments/environment";
import {User} from "../../../interfaces";
import {Subject} from "rxjs";
import {combineLatest} from "rxjs/observable/combineLatest";
import {APIService, EthereumService, MessageBoxService} from "../../../services";
import {TranslateService} from "@ngx-translate/core";
import {PoolService} from "../../../services/pool.service";
import * as Web3 from "web3";

@Component({
  selector: 'app-launch-node-page',
  templateUrl: './launch-node-page.component.html',
  styleUrls: ['./launch-node-page.component.sass']
})
export class LaunchNodePageComponent implements OnInit, OnDestroy {

  public getLiteWalletLink = environment.getLiteWalletLink;
  public sumusAddress: string = null;
  public ethAddress: string | null;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isMetamask: boolean = false;
  public tokenAmount: number = 0;
  public etherscanUrl = environment.etherscanUrl;
  public user: User;
  public isAuthenticated: boolean = false;
  public agreeCheck: boolean = false;
  public allowedMMNetwork = environment.MMNetwork;
  public allowedWalletNetwork = environment.walletNetwork;
  public currentWalletNetwork;
  public etherscanContractUrl = environment.etherscanContractUrl;
  public poolContract = environment.EthPoolContractAddress;
  public isInvalidMMNetwork: boolean = true;
  public isInvalidWalletNetwork: boolean = true;
  public userStake: number;
  public userFrozenStake: number;
  public errors = {
    haveToHold: false,
    stakeSent: false
  };

  private destroy$: Subject<boolean> = new Subject<boolean>();
  private timeoutPopUp;
  private liteWallet = window['GoldMint'];
  private checkLiteWalletInterval;
  private sub1: Subscription;
  private Web3 = new Web3();

  public system: string = '';
  public locale: string;
  public videoUrl: any;
  public osList = [
    {label: 'Windows', value: 'windows'},
    {label: 'Linux', value: 'linux'}
    // {label: 'MacOS', value: 'mac'}
  ];
  public direction: string;
  public isSent: boolean = false;

  public systemMap = {
    'windowsru': {
      video: '_9BUs5GKwU8',
      text: 'https://github.com/Goldmint/sumus-docs/blob/master/instruction_for_win_RUS.pdf'
    },
    'windowsen': {
      video: '4tLqYb_iD00',
      text: 'https://github.com/Goldmint/sumus-docs/blob/master/instruction_for_win_ENG.pdf'
    },
    'macru': {
      video: '',
      text: ''
    },
    'macen': {
      video: '',
      text: ''
    },
    'linuxru' : {
      video: 'elyLVU3Chpo',
      text: 'https://github.com/Goldmint/sumus-docs/blob/master/instruction_for_linux_RUS.pdf'
    },
    'linuxen': {
      video: 'Wi7831BnO8o',
      text: 'https://github.com/Goldmint/sumus-docs/blob/master/instruction_for_linux_ENG.pdf'
    }
  }

  constructor(
    private deviceService: DeviceDetectorService,
    private userService: UserService,
    public sanitizer: DomSanitizer,
    private messageBox: MessageBoxService,
    private apiService: APIService,
    private ethService: EthereumService,
    private _cdRef: ChangeDetectorRef,
    private translate: TranslateService,
    private poolService: PoolService
  ) { }

  ngOnInit() {
    this.isAuthenticated = this.userService.isAuthenticated();
    this.initModal();
    this.getEthAddress();
    this.detectMetaMask();
    this.detectLiteWallet();

    this.isDataLoaded = true;
    this._cdRef.markForCheck();

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

    this.isAuthenticated && this.apiService.getProfile().subscribe(data => {
      this.user = data.data;
      this._cdRef.markForCheck();
    });

    const combined = combineLatest(
      this.poolService.getObsUserStake(),
      this.poolService.getObsUserFrozenStake()
    );

    combined.takeUntil(this.destroy$).subscribe((data: any) => {
      if (data[0] !== null && data[1] !== null) {
        this.userStake = +data[0];
        this.userFrozenStake = +data[1];

        if (this.userStake >= 10000 || this.userFrozenStake >= 10000) {
          this.errors.haveToHold = true;
        } else if (this.userStake - this.userFrozenStake == 0) {
          this.errors.stakeSent = true;
        } else {
          this.tokenAmount = this.userStake - this.userFrozenStake;
        }
        this._cdRef.markForCheck();
      }
    });

    this.system = this.deviceService.getDeviceInfo().os;
    this.userService.currentLocale.takeUntil(this.destroy$).subscribe(locale => {
      this.locale = locale;
      this.direction = this.system + this.locale;
      this.setVideoUrl();
    });
    this.direction = this.system + this.locale;
    this.setVideoUrl();
  }

  chooseSystem(os: string) {
    this.system = os;
    this.direction = this.system + this.locale;
    this.setVideoUrl();
  }

  setVideoUrl() {
    this.videoUrl = this.sanitizer.bypassSecurityTrustResourceUrl('https://www.youtube.com/embed/' + this.systemMap[this.direction].video);
  }

  getEthAddress() {
    this.ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(address => {
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
    if (window.hasOwnProperty('web3') || window.hasOwnProperty('ethereum')) {
      this.timeoutPopUp = setTimeout(() => {
        this.loading = false;
        !this.isMetamask && this.userService.showLoginToMMBox('HeadingMigration');
        this._cdRef.markForCheck();
      }, 3000);
    } else {
      setTimeout(() => {
        this.translate.get('MessageBox.MetaMask').subscribe(phrase => {
          this.messageBox.alert(phrase.Text, phrase.Heading);
        });
      }, 200);
    }
  }

  detectLiteWallet() {
    if (window.hasOwnProperty('GoldMint')) {
      this.liteWallet.getAccount().then(res => {
        this.sumusAddress = res.length ? res[0] : null;

        !this.sumusAddress && setTimeout(() => {
          this.userService.showLoginToLiteWallet();
        }, 200);
        this._cdRef.markForCheck();
      });
    } else {
      setTimeout(() => {
        this.translate.get('MessageBox.LiteWallet').subscribe(phrase => {
          this.messageBox.alert(`
            <div>${phrase.Text} <a href="${this.getLiteWalletLink}" target="_blank">Goldmint Lite Wallet</a></div>
      `, phrase.Heading);
        });
      }, 200);
    }
  }

  initModal() {
    this.poolService.getSuccessMNTTokenLink$.takeUntil(this.destroy$).subscribe((hash: string) => {
      if (hash) {
        this.successModal(hash);
        this.isSent = true;
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
      this.liteWallet.getCurrentNetwork().then(res => {
        if (this.currentWalletNetwork != res) {
          this.currentWalletNetwork = res;
          if (res !== this.allowedWalletNetwork) {
            this.userService.showInvalidNetworkModal('InvalidNetworkWallet', this.allowedWalletNetwork);
            this.isInvalidWalletNetwork = true;
          } else {
            this.isInvalidWalletNetwork = false;
          }
          this._cdRef.markForCheck();
        }
      });

      this.liteWallet.getAccount().then(res => {
        if (this.sumusAddress != res[0]) {
          this.sumusAddress = res.length ? res[0] : null;
          this._cdRef.markForCheck();
        }
      });
    }
  }

  onSubmit() {
    if (!this.isAuthenticated) {
      this.messageBox.authModal();
      return;
    }

    let firstLoad = true;
    this.sub1 && this.sub1.unsubscribe();
    this.sub1 = this.ethService.getObservableGasPrice().takeUntil(this.destroy$).subscribe((price) => {
      if (price && firstLoad) {
        firstLoad = false;
        this.poolService.freezeStake(this.Web3.fromAscii(this.sumusAddress), this.ethAddress, +price * Math.pow(10, 9));
      }
    });

    this._cdRef.markForCheck();
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    clearTimeout(this.timeoutPopUp);
    clearInterval(this.checkLiteWalletInterval);
    this.sub1 && this.sub1.unsubscribe();
  }

}
