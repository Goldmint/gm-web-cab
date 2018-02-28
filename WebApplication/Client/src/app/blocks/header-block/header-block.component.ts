import { Component, OnInit, OnDestroy, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter } from '@angular/core';
import { zip } from 'rxjs/observable/zip';
import { BigNumber } from "bignumber.js"
// import { takeUntil } from 'rxjs/operators';

// import * as Web3 from 'web3';

import { User } from '../../interfaces';
import { UserService, APIService, MessageBoxService, EthereumService, GoldrateService } from '../../services';

@Component({
  selector: 'app-header',
  templateUrl: './header-block.component.html',
  styleUrls: ['./header-block.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderBlockComponent implements OnInit, OnDestroy {

  // private ngUnsubscribe: Subject<void> = new Subject<void>();

  public gold_usd_rate: number;
  public user: User;
  public locale: string;
  public signupButtonBlur = new EventEmitter<boolean>();

  metamaskAccount: string = null;
  goldBalance: string|null = null;
  hotGoldBalance: string|null = null;
  usdBalance: number|null = null;

  constructor(
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService
  ) {
  }

  // ngOnChanges() {
  //   console.log('ngOnChanges');
  // }

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

    /*if (window.hasOwnProperty('web3')) {
      this._web3 = new Web3(window['web3'].currentProvider);
      this.metamaskAccount = this._web3.eth.accounts.length ? this._web3.eth.accounts[0] : undefined;

      console.log('MetaMask accounts: ', this._web3.eth.accounts);
    } else {
      // this._messageBox.alert('You need to get a web3 browser. Or install <a href="https://metamask.io">MetaMask</a> to continue.');
      console.info('You need to get a web3 browser. Or install <a href="https://metamask.io">MetaMask</a> to continue.');
    }*/
  }

  // ngDoCheck() {
  //   console.log('ngDoCheck');
  // }

  // ngAfterContentInit() {
  //   console.log('ngAfterContentInit');
  // }

  // ngAfterContentChecked() {
  //   console.log('ngAfterContentChecked');
  // }

  // ngAfterViewInit() {
  //   console.log('ngAfterViewInit');
  // }

  // ngAfterViewChecked() {
  //   console.log('ngAfterViewChecked');
  // }

  ngOnDestroy() {
    // console.log('ngOnDestroy');

    // this.ngUnsubscribe.next();
    // this.ngUnsubscribe.complete();
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
