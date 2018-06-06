import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef,
  OnDestroy
} from '@angular/core';
import { User } from '../../interfaces';
import { UserService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import {Subject} from "rxjs/Subject";
import {TranslateService} from "@ngx-translate/core";
import {NavigationEnd, Router} from "@angular/router";

@Component({
  selector: 'app-header',
  templateUrl: './header-block.component.html',
  styleUrls: ['./header-block.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderBlockComponent implements OnInit, OnDestroy {

  public gold_usd_rate: number;
  public gold_eth_rate: number;
  public user: User;
  public locale: string;
  public wallets = [
    {id: 'metamask', name: 'METAMASK', account: ''}
  ];
  public activeWallet: Object = this.wallets[0];

  public metamaskAccount: string = null;
  public goldBalance: string|null = '0';
  public hotGoldBalance: string|null = null;
  public shortAdr: string;
  public isShowMobileMenu: boolean = false;
  public isMobile: boolean = false;
  private destroy$: Subject<boolean> = new Subject<boolean>();


  constructor(
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService,
    private _translate: TranslateService,
    private router: Router
  ) {
  }

  ngOnInit() {
    if (window.innerWidth > 767) {
      this.isMobile = this.isShowMobileMenu = false;
    } else {
      this.isMobile = true;
    }
    this._cdRef.markForCheck();

    window.onresize = () => {
      this._userService.windowSize$.next(window.innerWidth);

      if (window.innerWidth > 767) {
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
        this.goldBalance = '0';
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

    this._ethService.getObservableGoldBalance().takeUntil(this.destroy$).subscribe(bal => {
      if (bal != null) {
        this.goldBalance = bal.toString().replace(/^(\d+\.\d\d)\d+$/, '$1');
        this._cdRef.markForCheck();
      }
    });

    this._ethService.getObservableHotGoldBalance().takeUntil(this.destroy$).subscribe(bal => {
      if (bal != null) {
        this.hotGoldBalance = bal.toString().replace(/^(\d+\.\d\d)\d+$/, '$1');
        this._cdRef.markForCheck();
      }
    });

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

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
