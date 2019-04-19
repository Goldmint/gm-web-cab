import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef,
  OnDestroy, ViewChild
} from '@angular/core';
import { User } from '../../interfaces';
import { UserService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import {Subject} from "rxjs/Subject";
import {TranslateService} from "@ngx-translate/core";
import {NavigationEnd, Router} from "@angular/router";
import {APIService} from "../../services/api.service";
import {BigNumber} from "bignumber.js";
import {BsModalService} from "ngx-bootstrap";

@Component({
  selector: 'app-header',
  templateUrl: './header-block.component.html',
  styleUrls: ['./header-block.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderBlockComponent implements OnInit, OnDestroy {

  @ViewChild('depositModal') depositModal;

  public gold_usd_rate: number = 0;
  public gold_eth_rate: number = 0;
  public user: User;
  public locale: string;
  public wallets = [
    {id: 'metamask', name: 'METAMASK', account: ''}
  ];
  public activeWallet: Object = this.wallets[0];
  public sumusNetwork: string = 'MainNet';
  public metamaskAccount: string = null;
  public goldBalance: number = 0;
  public hotGoldBalance: string|null = null;
  public shortAdr: string;
  public isShowMobileMenu: boolean = false;
  public isMobile: boolean = false;
  public isLoggedInToMM: boolean = true;
  public isBalanceLoaded: boolean = false;
  public sumusAddress: string = '';
  public modalRef;

  private destroy$: Subject<boolean> = new Subject<boolean>();
  private updateGoldBalanceInterval;
  private liteWallet = window['GoldMint'];

  constructor(
    private _apiService: APIService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService,
    private _translate: TranslateService,
    private router: Router,
    private _modalService: BsModalService
  ) {
  }

  ngOnInit() {
    if (window.innerWidth > 992) {
      this.isMobile = this.isShowMobileMenu = false;
    } else {
      this.isMobile = true;
    }
    this._cdRef.markForCheck();

    window.onresize = () => {
      this._userService.windowSize$.next(window.innerWidth);

      if (window.innerWidth > 992) {
        this.isMobile = this.isShowMobileMenu = false;
        document.body.style.overflow = 'visible';
      } else {
        this.isMobile = true;
      }
      this._cdRef.markForCheck();
    };

    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.isShowMobileMenu = false;
        document.body.style.overflow = 'visible';
        this._cdRef.markForCheck();
      }
    });

    this._ethService.getObservableSumusAccount().takeUntil(this.destroy$).subscribe(data => {
      if (data) {
        if (+data.sumusGold > 0) {
          const balance = +data.sumusGold / Math.pow(10, 18);
          this.goldBalance = Math.floor(balance * 1000) / 1000;
        }
        this.sumusAddress = data.sumusWallet;
        this.isBalanceLoaded = true;
        this._cdRef.markForCheck();
      }
    });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(data => {
      data && (this.gold_usd_rate = data.gold) && (this.gold_eth_rate = data.eth);
      this._cdRef.markForCheck();
    });

    this._userService.currentUser.takeUntil(this.destroy$).subscribe(currentUser => {
      this.user = currentUser;
      this._cdRef.markForCheck();
    });

    this._userService.currentLocale.takeUntil(this.destroy$).subscribe(currentLocale => {
      this.locale = currentLocale;
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      if (this.metamaskAccount && !ethAddr) {
        // this.goldBalance = '0';
      }
      this.metamaskAccount = ethAddr;
      this.activeWallet = this.wallets[0];
      // !this.metamaskAccount && this.activeWallet['id'] === 'metamask' && (this.activeWallet = this.wallets[0]);
      this.showShortAccount();
      this.wallets.forEach(item => {
        item.account = item.id === 'metamask' ? this.shortAdr : '';
      });
      this._cdRef.markForCheck();
    });

    // this._ethService.getObservableGoldBalance().takeUntil(this.destroy$).subscribe(bal => {
    //   if (bal != null) {
    //     this.goldBalance = bal.toString().replace(/^(\d+\.\d{2,)\d+$/, '$1');
    //     this._cdRef.markForCheck();
    //   }
    // });

    // this._ethService.getObservableHotGoldBalance().takeUntil(this.destroy$).subscribe((bal: any) => {
    //   this.hotGoldBalance = bal;
    //   if (bal != null) {
    //     this.hotGoldBalance = bal.toString().replace(/^(\d+\.\d\d)\d+$/, '$1');
    //   }
    //   this._cdRef.markForCheck();
    // });

    // this.sumusNetwork = localStorage.getItem('gmint_sumus_network') ?
    //                     localStorage.getItem('gmint_sumus_network') : 'MainNet';
    // this._apiService.transferCurrentSumusNetwork.next(this.sumusNetwork);

    if (window.hasOwnProperty('web3') || window.hasOwnProperty('ethereum')) {
      setTimeout(() => {
        !this.isLoggedIn() && !this.metamaskAccount && (this.isLoggedInToMM = false);
        this._cdRef.markForCheck();
      }, 3000);
    }

    this._userService.currentWallet = this.activeWallet;
    this._cdRef.markForCheck();
  }

  /*onWalletSwitch(wallet) {
    if (wallet.id === 'metamask' && !this.metamaskAccount) {
      this._translate.get('MessageBox.MetaMask').subscribe(phrase => {
        this._messageBox.alert(phrase.Text, phrase.Heading);
      });
      return;
    }

    this._userService.currentWallet = this.activeWallet = wallet;
    this._userService.onWalletSwitch(wallet);

    this.showShortAccount();
    this._cdRef.markForCheck()();
  }*/

  public showShortAccount() {
    this.shortAdr = this.metamaskAccount ? ' (' + this.metamaskAccount.slice(0, 5) + ')...' : '';
  }

  public logout(e) {
    e.preventDefault();

    this._translate.get('MessageBox.logOut').subscribe(phrase => {
      this._messageBox.confirm(phrase)
        .subscribe(confirmed => {
          if (confirmed) {
            this._userService.logout(e);
            this._cdRef.markForCheck();
          }
        });
    });
  }

  public isLoggedIn() {
    return this._userService.isAuthenticated();
  }

  toggleMobileMenu(e) {
    this.isShowMobileMenu = !this.isShowMobileMenu;
    document.body.style.overflow = this.isShowMobileMenu ? 'hidden' : 'visible';
    e.stopPropagation();
    this._cdRef.markForCheck();
  }

  changeSumusNetwork(network) {
    this.sumusNetwork = network;
    localStorage.setItem('gmint_sumus_network', network);
    this._apiService.transferCurrentSumusNetwork.next(network);
    this._cdRef.markForCheck();
  }

  openDepositModal() {
    this.modalRef = this._modalService.show(this.depositModal, {class: 'message-box'});
  }

  openLiteWallet() {
    if (window.hasOwnProperty('GoldMint')) {
      this.liteWallet.getAccount().then(res => {
        !res.length && this._userService.showLoginToLiteWalletModal();
        res.length && this.liteWallet.openSendTokenPage(this.sumusAddress, 'GOLD').then(() => {});
      });
    } else {
      this._userService.showGetLiteWalletModal();
    }
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    clearInterval(this.updateGoldBalanceInterval);
  }

}
