import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {APIService, UserService} from "../../../services";
import {EthereumService} from "../../../services/ethereum.service";
import {Subject} from "rxjs/Subject";
import {BigNumber} from "bignumber.js";
import * as Web3 from "web3";
import {MessageBoxService} from "../../../services/message-box.service";
import {Subscription} from "rxjs/Subscription";
import {combineLatest} from "rxjs/observable/combineLatest";
import {TranslateService} from "@ngx-translate/core";
import {interval} from "rxjs/observable/interval";
import {environment} from "../../../../environments/environment";
import {User} from "../../../interfaces/user";

@Component({
  selector: 'app-token-migration-page',
  templateUrl: './token-migration-page.component.html',
  styleUrls: ['./token-migration-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TokenMigrationPageComponent implements OnInit, OnDestroy {

  public getLiteWalletLink = environment.getLiteWalletLink;
  public sumusAddress: string = null;
  public ethMigrationAddress: string = '';
  public sumusMigrationAddress: string = '';
  public direction: string;

  public ethAddress: string | null;
  public goldBalanceBigNumber: BigNumber = null;
  public mntpBalanceBigNumber: BigNumber = null;
  public goldBalance: number;
  public mntpBalance: number;
  public tokenModel: {
    type: 'GOLD'|'MNTP'
  };
  public directionModel: {
    type: 'eth'|'sumus'
  };
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isSumusSuccess: boolean = false;
  public invalidAmount: boolean = false;
  public balanceError: boolean = false;
  public isFirstSend: boolean = true;
  public isMetamask: boolean = false;
  public tokenAmount: number;
  public currentBalance: number;
  public isAddressLoaded: boolean = false;
  public etherscanUrl = environment.etherscanUrl;
  public user: User;
  public isAuthenticated: boolean = false;
  public agreeCheck: boolean = false;
  public isMigrationDuplicateRequest: boolean = false;

  private Web3: Web3 = new Web3();
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private sub1: Subscription;
  private timeoutPopUp;
  private liteWallet = window['GoldMint'];

  constructor(
    private messageBox: MessageBoxService,
    private apiService: APIService,
    private ethService: EthereumService,
    private userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private translate: TranslateService
    ) { }

  ngOnInit() {
    this.isAuthenticated = this.userService.isAuthenticated();
    this.loading = true;
    this.tokenModel = {
      type: 'GOLD'
    };
    this.directionModel = {
      type: 'eth'
    };
    this.direction = this.tokenModel.type + this.directionModel.type;

    if (!this.isAuthenticated) {
      this.getDataForMigration(false);
      this.isDataLoaded = this.isAddressLoaded = true;
      this._cdRef.markForCheck();
    }

    interval(500).subscribe(this.checkLiteWallet.bind(this));

    this.isAuthenticated && this.apiService.getProfile().subscribe(data => {
      this.user = data.data;
      this.isDataLoaded = true;

      if (this.user.verifiedL0) {
        this.getDataForMigration(true);
      }
      this._cdRef.markForCheck();
    });
  }

  getDataForMigration(isAuthenticated) {
    this.initGoldMigrationModal();
    this.initMntpMigrationModal();

    this.getTokenBalance();
    this.getEthAddress();
    isAuthenticated && this.getMigrationStatus();

    this.detectMetaMask();
    this.detectLiteWallet();
  }

  getTokenBalance() {
    const combined = combineLatest(
      this.ethService.getObservableGoldBalance(),
      this.ethService.getObservableMntpBalance()
    )

    combined.takeUntil(this.destroy$).subscribe((data) => {
      if(
        (data[0] !== null && data[1] !== null) && (
          (this.goldBalanceBigNumber === null || !this.goldBalanceBigNumber.eq(data[0]))
          ||
          (this.mntpBalanceBigNumber === null || !this.mntpBalanceBigNumber.eq(data[1]))
        )
      ) {
        this.goldBalanceBigNumber = data[0];
        this.mntpBalanceBigNumber = data[1];
        this.goldBalance = +data[0].decimalPlaces(6, BigNumber.ROUND_DOWN);
        this.mntpBalance = +data[1].decimalPlaces(6, BigNumber.ROUND_DOWN);

        this.chooseToken();
        this.loading = false;
        this._cdRef.markForCheck();
      }
    });
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

  getMigrationStatus() {
    this.apiService.getMigrationStatus().subscribe((data: any) => {
      this.ethMigrationAddress = data.data.ethereum.migrationAddress;
      this.sumusMigrationAddress = data.data.sumus.migrationAddress;
      this.isAddressLoaded = true;
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

  initGoldMigrationModal() {
    this.ethService.getSuccessMigrationGoldLink$.takeUntil(this.destroy$).subscribe((hash: string) => {
      hash && this.successMigrationModal(hash);
    });
  }

  initMntpMigrationModal() {
    this.ethService.getSuccessMigrationMntpLink$.takeUntil(this.destroy$).subscribe((hash: string) => {
      hash && this.successMigrationModal(hash);
    });
  }

  successMigrationModal(hash: string) {
    this.translate.get('PAGES.MasterNode.MigrationPage.SuccessModal').subscribe(phrases => {
      this.messageBox.alert(`
         <div class="text-center">
            <div class="font-weight-500 mb-2">${phrases.Heading}</div>
            <div>${phrases.Hash}</div>
            <div class="mb-2 migration-hash">${hash}</div>
            <a href="${this.etherscanUrl}${hash}" target="_blank">${phrases.Link}</a>
        </div>
       `);
    });
  }

  successSumusMigrationModal(digest: string) {
    this.translate.get('PAGES.MasterNode.MigrationPage.SuccessSumusModal').subscribe(phrases => {
      this.messageBox.alert(`
         <div class="text-center">
            <div class="font-weight-500 mb-2">${phrases.Heading}</div>
            <div>${phrases.Hash}</div>
            <div class="mb-2 migration-hash">${digest}</div>
            <a href="${location.origin}/#/scanner/tx/${digest}" target="_blank">${phrases.Link}</a>
        </div>
       `);
    });
  }

  checkLiteWallet() {
    if (window.hasOwnProperty('GoldMint')) {
      this.liteWallet.getAccount().then(res => {
        if (this.sumusAddress != res[0]) {
          this.sumusAddress = res.length ? res[0] : null;
          this._cdRef.markForCheck();
        }
      });
    }
  }

  chooseToken() {
    this.currentBalance = this.tokenAmount = this.tokenModel.type === 'GOLD' ? this.goldBalance : this.mntpBalance;
    this.isSumusSuccess = this.isMigrationDuplicateRequest = this.agreeCheck = false;
    this.chooseDirection();
  }

  chooseDirection() {
    this.isMigrationDuplicateRequest = this.agreeCheck = false;

    this.direction = this.tokenModel.type + this.directionModel.type;
    if (this.direction === 'GOLDsumus' || this.direction === 'MNTPsumus') {
      this.invalidAmount = this.balanceError = false;
    } else {
      this.invalidAmount = !this.tokenAmount ? true : false;
      this.checkBalance();
    }
    this._cdRef.markForCheck();
  }

  onCopyData(input) {
    input.focus();
    input.setSelectionRange(0, input.value.length);
    document.execCommand("copy");
    input.setSelectionRange(0, 0);
  }

  substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d{1,6})\d*(?:(\.\d{0,6})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  checkBalance() {
    this.balanceError = this.tokenAmount > this.currentBalance ? true : false;
  }

  changeValue(e) {
    e.target.value = this.substrValue(e.target.value);
    e.target.setSelectionRange(e.target.value.length, e.target.value.length);
    this.tokenAmount = +e.target.value;
    this.invalidAmount = !this.tokenAmount ? true : false;
    this.checkBalance();
    this._cdRef.markForCheck();
  }

  setTokenBalance(percent) {
    let balance = this.tokenModel.type === 'GOLD' ? this.goldBalance : this.mntpBalance;

    const value = this.substrValue(+balance * percent);
    this.tokenAmount = +value;
    this.invalidAmount = !this.tokenAmount ? true : false;
    this._cdRef.markForCheck();
  }

  sendSumusTransaction() {
    this.liteWallet.getBalance(this.sumusAddress).then(res => {
      let balance = res,
          token = this.tokenModel.type === 'GOLD' ? 'GOLD' : 'MNT',
          amount = this.tokenModel.type === 'GOLD' ? +balance.gold : +balance.mint;

      this.liteWallet.sendTransaction(this.sumusMigrationAddress, token, amount).then(digest => {
        digest && this.successSumusMigrationModal(digest);
      });
    });
  }

  onSubmit() {
    if (!this.isAuthenticated) {
      this.messageBox.authModal();
      return;
    }

    let methodsMap = {
      GOLDeth: this.apiService.goldMigrationEth,
      GOLDsumus: this.apiService.goldMigrationSumus,
      MNTPeth: this.apiService.mintMigrationEth,
      MNTPsumus: this.apiService.mintMigrationSumus
    };
    this.isFirstSend = true;
    this.loading = true;
    this._cdRef.markForCheck();

    methodsMap[this.direction]
      .call(this.apiService, this.sumusAddress, this.ethAddress).subscribe(() => {
        this.ethService.getObservableGasPrice().subscribe((price) => {
          if (price !== null && this.isFirstSend) {
            this.isFirstSend = false;
            this.isMigrationDuplicateRequest = false;

            const amount = this.Web3.toWei(this.tokenAmount);
            if (this.direction === 'GOLDeth') {
              this.ethService.goldTransferMigration(this.ethAddress, this.ethMigrationAddress, amount, +price * Math.pow(10, 9));
            } else if (this.direction === 'MNTPeth') {
              this.ethService.mntpTransferMigration(this.ethAddress, this.ethMigrationAddress, amount, +price * Math.pow(10, 9));
            } else {
              this.isSumusSuccess = true;
              this.sendSumusTransaction();
              this._cdRef.markForCheck();
            }
            this.loading = false;
            this._cdRef.markForCheck();
          }
        });
      }, (error) => {
        error.error.errorCode === 106 && (this.isMigrationDuplicateRequest = true);
        this.loading = false;
        this._cdRef.markForCheck();
    });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    this.sub1 && this.sub1.unsubscribe();
    clearTimeout(this.timeoutPopUp);
  }

}
