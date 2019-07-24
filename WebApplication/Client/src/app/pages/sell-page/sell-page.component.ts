import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding, OnDestroy
} from '@angular/core';
import {UserService, APIService, MessageBoxService, EthereumService} from '../../services';
import { Observable } from "rxjs/Observable";
import {TranslateService} from "@ngx-translate/core";
import {User} from "../../interfaces/user";
import {TFAInfo} from "../../interfaces";
import {Subject} from "rxjs/Subject";
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-sell-page',
  templateUrl: './sell-page.component.html',
  styleUrls: ['./sell-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SellPageComponent implements OnInit, OnDestroy {
  @HostBinding('class') class = 'page';

  public loading = true;
  public user: User;
  public tfaInfo: TFAInfo;
  public isMetamask = true;
  public tradingStatus: {creditCardSellingAllowed: boolean, ethAllowed: boolean};
  public blockedCountriesList = [];
  public isBlockedCountry: boolean = false;
  public MMNetwork = environment.MMNetwork;
  public isInvalidNetwork: boolean = false;
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

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.isMetamask = !ethAddr ? false : true;
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
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
