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
import * as bs58 from 'bs58';
import * as CRC32 from 'crc-32';
import {MessageBoxService} from "../../../services/message-box.service";
import {Status} from "../../../interfaces/status";
import {Subscription} from "rxjs/Subscription";
import {combineLatest} from "rxjs/observable/combineLatest";
import {TranslateService} from "@ngx-translate/core";

@Component({
  selector: 'app-token-migration-page',
  templateUrl: './token-migration-page.component.html',
  styleUrls: ['./token-migration-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TokenMigrationPageComponent implements OnInit, OnDestroy {

  public sumusAddress: string = '';
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
  public isSumusSuccess: boolean = false;
  public invalidAmount: boolean = false;
  public balanceError: boolean = false;
  public isFirstSend: boolean = true;
  public isMetamask: boolean = false;
  public isValidSumusAddress: boolean = false;
  public tokenAmount: number;
  public currentBalance: number;

  private Web3: Web3 = new Web3();
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private sub1: Subscription;
  private timeoutPopUp;

  constructor(
    private messageBox: MessageBoxService,
    private apiService: APIService,
    private ethService: EthereumService,
    private userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private translate: TranslateService
    ) { }

  ngOnInit() {
    this.loading = true;
    this.tokenModel = {
      type: 'GOLD'
    };
    this.directionModel = {
      type: 'eth'
    };
    this.direction = this.tokenModel.type + this.directionModel.type;

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

    if (window.hasOwnProperty('web3')) {
      this.timeoutPopUp = setTimeout(() => {
        this.loading = false;
        !this.isMetamask && this.userService.showLoginToMMBox();
        this._cdRef.markForCheck();
      }, 3000);
    } else {
      setTimeout(() => {
        this.translate.get('MESSAGE.MetaMask').subscribe(phrase => {
          this.messageBox.alert(phrase.Text, phrase.Heading);
        });
      }, 200);
    }

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

    /*this.apiService.getStatus().subscribe((data: Status) => {
      this.ethMigrationAddress = data.data.ethereum.migrationAddress;
      this.sumusMigrationAddress = data.data.sumus.migrationAddress;
    });*/
  }

  chooseToken() {
    this.currentBalance = this.tokenAmount = this.tokenModel.type === 'GOLD' ? this.goldBalance : this.mntpBalance;
    this.chooseDirection();
  }

  chooseDirection() {
    this.direction = this.tokenModel.type + this.directionModel.type;
    if (this.direction === 'GOLDsumus' || this.direction === 'MNTPsumus') {
      this.invalidAmount = this.balanceError = false;
    } else {
      this.invalidAmount = !this.tokenAmount ? true : false;
      this.checkBalance();
    }
    this._cdRef.markForCheck();
  }

  checkSumusAddressValidity(address: string) {
    let bytes;
    try {
      bytes = bs58.decode(address);
    } catch (e) {
      this.isValidSumusAddress = false;
      return
    }

    if (bytes.length <= 4 || this.sumusAddress.length <= 4) {
      this.isValidSumusAddress = false;
      return
    }

    let payloadCrc = CRC32.buf(bytes.slice(0, -4));
    let crcBytes = bytes.slice(-4);
    let crc = crcBytes[0] | crcBytes[1] << 8 | crcBytes[2] << 16 | crcBytes[3] << 24;

    this.isValidSumusAddress = payloadCrc === crc;
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

  /*onSubmit() {
    let methodsMap = {
      GOLDeth: this.apiService.goldMigrationEth,
      GOLDsumus: this.apiService.goldMigrationSumus,
      MNTPeth: this.apiService.mintMigrationEth,
      MNTPsumus: this.apiService.mintMigrationSumus
    };
    this.isFirstSend = true;

    methodsMap[this.direction]
      .call(this.apiService, this.sumusAddress, this.ethAddress).subscribe(() => {
        this.loading = true;
        this.ethService.getObservableGasPrice().subscribe((price) => {
          if (price !== null && this.isFirstSend) {
            this.isFirstSend = false;
            const amount = this.Web3.toWei(this.tokenAmount);
            if (this.direction === 'GOLDeth') {
              this.ethService.goldTransferMigration(this.ethAddress, this.ethMigrationAddress, amount, +price * Math.pow(10, 9));
            } else if (this.direction === 'MNTPeth') {
              this.ethService.mntpTransferMigration(this.ethAddress, this.ethMigrationAddress, amount, +price * Math.pow(10, 9));
            } else {
              this.isSumusSuccess = true;
              this._cdRef.markForCheck();
            }
            this.loading = false;
            this._cdRef.markForCheck();
          }
        });
      }, error => {
        this.loading = false;
        this.messageBox.alert(error.error.errorDesc);
        this._cdRef.markForCheck();
    });
  }*/

  ngOnDestroy() {
    this.destroy$.next(true);
    this.sub1 && this.sub1.unsubscribe();
    clearTimeout(this.timeoutPopUp);
  }

}
