import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef,
  OnDestroy
} from '@angular/core';
import { User } from '../../interfaces';
import { UserService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import {Subject} from "rxjs/Subject";
import {TranslateService} from "@ngx-translate/core";
import {ActivatedRoute, NavigationEnd, Router} from "@angular/router";
import {APIService} from "../../services/api.service";
import {environment} from "../../../environments/environment";
import {CommonService} from "../../services/common.service";

@Component({
  selector: 'app-header',
  templateUrl: './header-block.component.html',
  styleUrls: ['./header-block.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderBlockComponent implements OnInit, OnDestroy {

  public gold_usd_rate: number = 0;
  public gold_eth_rate: number = 0;
  public user: User;
  public locale: string;
  public isShowMobileMenu: boolean = false;
  public isMobile: boolean = false;
  public getLiteWalletLink;
  public menuRoutes = {
    masterNode: ['/master-node', '/ethereum-pool', '/buy-mntp', '/swap-mntp'],
    scanner: ['/scanner', '/nodes', '/pawnshop-loans']
  };
  public activeMenuItem: string;
  public networkList;
  public buySellCyberbridgeLink = environment.buySellCyberbridgeLink;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private _apiService: APIService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService,
    private _translate: TranslateService,
    private router: Router,
    private route: ActivatedRoute,
    private commonService: CommonService
  ) {
  }

  ngOnInit() {
    let isFirefox = typeof window['InstallTrigger'] !== 'undefined';
    this.getLiteWalletLink = isFirefox ? environment.getLiteWalletLink.firefox : environment.getLiteWalletLink.chrome;

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
        this.checkActiveMenuItem(event.urlAfterRedirects);
        this.isShowMobileMenu = false;
        document.body.style.overflow = 'visible';
        window.scrollTo(0, 0);

        this._cdRef.markForCheck();
      }
    });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(data => {
      data && (this.gold_usd_rate = data.gold) && (this.gold_eth_rate = data.eth);
      this._cdRef.markForCheck();
    });

    this._userService.currentLocale.takeUntil(this.destroy$).subscribe(currentLocale => {
      this.locale = currentLocale;
      this._cdRef.markForCheck();
    });

    this._cdRef.markForCheck();
  }

  checkActiveMenuItem(route: string) {
    this.activeMenuItem = '';
    for (let key in this.menuRoutes) {
      this.menuRoutes[key].forEach(url => {
        route.indexOf(url) >= 0 && (this.activeMenuItem = key);
      });
    }
    this.commonService.getActiveMenuItem.next(this.activeMenuItem);
    this._cdRef.markForCheck();
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
