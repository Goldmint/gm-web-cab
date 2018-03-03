import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { User } from '../../interfaces';
import { UserService, MessageBoxService, EthereumService, GoldrateService } from '../../services';

@Component({
  selector: 'app-header',
  templateUrl: './header-block.component.html',
  styleUrls: ['./header-block.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderBlockComponent implements OnInit {

  public gold_usd_rate: number;
  public user: User;
  public locale: string;
  public wallets = [
    {id: 'hot', name: 'HOT WALLET'},
    {id: 'metamask', name: 'METAMASK'},
  ];
  public activeWallet: Object = this.wallets[0];


  metamaskAccount: string = null;
  goldBalance: string|null = null;
  hotGoldBalance: string|null = null;
  usdBalance: number|null = null;
  shortAdr: string;


  constructor(
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService
  ) {
  }

  ngOnInit() {
    this._goldrateService.getObservableRate().subscribe(data => {
      this.gold_usd_rate = data;
      this._cdRef.detectChanges();
    });

    this._userService.currentUser.subscribe(currentUser => {
      this.user = currentUser;
      this._cdRef.detectChanges();
    });

    this._userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
      this._cdRef.detectChanges();
    });

    this._ethService.getObservableEthAddress().subscribe(ethAddr => {
      this.metamaskAccount = ethAddr;
      !this.metamaskAccount && this.activeWallet['id'] === 'metamask' && (this.activeWallet = this.wallets[0]);
      this._cdRef.detectChanges();
    });

    this._ethService.getObservableGoldBalance().subscribe(bal => {
      if (bal != null) {
        this.goldBalance = bal.toString().replace(/^(\d+\.\d\d)\d+$/, '$1');
        this._cdRef.detectChanges();
      }
    });

    this._ethService.getObservableHotGoldBalance().subscribe(bal => {
      if (bal != null) {
        this.hotGoldBalance = bal.toString().replace(/^(\d+\.\d\d)\d+$/, '$1');
        this._cdRef.detectChanges();
      }
    });

    this._ethService.getObservableUsdBalance().subscribe(bal => {
      this.usdBalance = bal;
      this._cdRef.detectChanges();
    });

    this._userService.currentWallet = this.activeWallet;
    this._cdRef.detectChanges();
  }

  onWalletSwitch(wallet) {
    if (wallet.id === 'metamask' && !this.metamaskAccount) {
      this._messageBox.alert(`MetaMask is a bridge that allows you to visit the distributed web of tomorrow in your browser today. It allows you to run Ethereum dApps right in your browser without running a full Ethereum node. Get <a href="https://metamask.io" target="_blank">Metamask</a>`,'What is Metamask?');
      return;
    }

    this._userService.currentWallet = this.activeWallet = wallet;
    this._userService.onWalletSwitch(wallet);

    if (this.metamaskAccount && this.activeWallet['id'] === 'metamask') {
      this.shortAdr = this.metamaskAccount.slice(0, 5) + '...';
    } else {
      this.shortAdr = '';
    }
    this._cdRef.detectChanges();
  }

  showGoldRateInfo() {
    this._messageBox.alert(`${this.gold_usd_rate}`);
    this._cdRef.detectChanges();
  }

  private logout(e) {
    e.preventDefault();

    this._messageBox.confirm('Are you sure you want to log out?')
      .subscribe(confirmed => {
        if (confirmed) {
          this._userService.logout(e);
          this._cdRef.detectChanges();
        }
      });
  }

  public isLoggedIn() {
    return this._userService.isAuthenticated();
  }

}
