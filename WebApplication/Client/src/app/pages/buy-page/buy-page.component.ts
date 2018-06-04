import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, OnDestroy, HostBinding, isDevMode
} from '@angular/core';
import {UserService, APIService, MessageBoxService, EthereumService} from '../../services';
import { TFAInfo } from '../../interfaces'
import { Observable } from "rxjs/Observable";
import { TranslateService } from "@ngx-translate/core";
import { User } from "../../interfaces/user";
import {Subject} from "rxjs/Subject";
import {environment} from "../../../environments/environment";

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
  public selectedWallet = 0;
  public isMetamask = true;
  public user: User;
  public tfaInfo: TFAInfo;
  public hasExtraRights: boolean = true;
  public tradingStatus: {creditCardAllowed: boolean, ethAllowed: boolean};

  private timeoutPopUp;
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _cdRef: ChangeDetectorRef,
    private _translate: TranslateService,
    private _ethService: EthereumService
  ) { }

  ngOnInit() {
    Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._apiService.getProfile(),
      this._apiService.getTradingStatus()
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1].data;
        this.tradingStatus = res[2].data.trading;
        this.loading = false;

        if (environment.detectExtraRights) {
          this.hasExtraRights = this.user.hasExtraRights;
        }

        if (!window.hasOwnProperty('web3') && this.user.verifiedL1) {
          this._translate.get('MessageBox.MetaMask').subscribe(phrase => {
            this._messageBox.alert(phrase.Text, phrase.Heading);
          });
        }

        this._cdRef.markForCheck();
      });

    if (window.hasOwnProperty('web3')) {
      this.timeoutPopUp = setTimeout(() => {
        !this.isMetamask && this.showLoginToMMPopUp()
      }, 3000);
    }

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;

    this._userService.onWalletSwitch$.takeUntil(this.destroy$).subscribe((wallet) => {
      if (wallet['id'] === 'hot') {
        this.selectedWallet = 0;
      } else {
        this.selectedWallet = 1;
      }
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.isMetamask = !ethAddr ? false : true;
      this._cdRef.markForCheck();
    });
  }

  showLoginToMMPopUp() {
    this._translate.get('MessageBox.LoginToMM').subscribe(phrase => {
      this._messageBox.alert(`
        <div class="text-center">${phrase.Text}</div>
        <div class="metamask-icon"></div>
        <div class="text-center mt-2 mb-2">MetaMask</div>
      `, phrase.HeadingBuy);
    });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    clearTimeout(this.timeoutPopUp);
  }

}
