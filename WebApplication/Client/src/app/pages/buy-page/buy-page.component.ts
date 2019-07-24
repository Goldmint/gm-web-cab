import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  HostBinding,
  OnDestroy,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {TFAInfo, User} from "../../interfaces";
import {TradingStatus} from "../../interfaces/trading-status";
import {environment} from "../../../environments/environment";
import {Observable, Subject} from "rxjs";
import {APIService, EthereumService, MessageBoxService, UserService} from "../../services";
import {TranslateService} from "@ngx-translate/core";

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
  public isMetamask = true;
  public user: User;
  public tfaInfo: TFAInfo;
  public tradingStatus: TradingStatus;
  public blockedCountriesList = [];
  public isBlockedCountry: boolean = false;
  public MMNetwork = environment.MMNetwork;
  public isInvalidNetwork: boolean = true;
  public isAuthenticated: boolean = false;

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
    this.isAuthenticated = this._userService.isAuthenticated();
    !this.isAuthenticated && (this.loading = false);

    this.isAuthenticated && Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._apiService.getProfile(),
      this._apiService.getTradingStatus(),
      this._apiService.getKYCProfile(),
      this._apiService.getBannedCountries()
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1].data;
        this.tradingStatus = res[2].data.trading;

        this.blockedCountriesList = res[4] ? res[4].data : [];
        this.isBlockedCountry = this.blockedCountriesList.indexOf(res[3].data['country']) >= 0;
        !this.isBlockedCountry && this._userService.getIPInfo().subscribe(data => {
          this.isBlockedCountry = this.blockedCountriesList.indexOf(data['country']) >= 0;
          this.loading = false;
          this._cdRef.markForCheck();
        });

        this.isBlockedCountry && (this.loading = false);
        this._cdRef.markForCheck();
      });

    this._ethService.getObservableNetwork().takeUntil(this.destroy$).subscribe(network => {
      if (network !== null) {
        if (network != this.MMNetwork.index) {
          this._userService.invalidNetworkModal(this.MMNetwork.name);
          this.isInvalidNetwork = true;
        } else {
          this.isInvalidNetwork = false;
        }
        this._cdRef.markForCheck();
      }
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.isMetamask = !ethAddr ? false : true;
      this._cdRef.markForCheck();
    });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
