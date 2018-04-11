import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding, OnDestroy
} from '@angular/core';
import { UserService, APIService, MessageBoxService } from '../../services';
import { Observable } from "rxjs/Observable";
import {Subscription} from "rxjs/Subscription";
import {TranslateService} from "@ngx-translate/core";
import {User} from "../../interfaces/user";
import {TFAInfo} from "../../interfaces";

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
  public selectedWallet = 0;
  public user: User;
  public tfaInfo: TFAInfo;
  private sub1: Subscription;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _cdRef: ChangeDetectorRef,
    private _translate: TranslateService
  ) { }

  ngOnInit() {
    Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._userService.currentUser
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1];
        this.loading = false;
        this._cdRef.detectChanges();
      });

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;

    this.sub1 = this._userService.onWalletSwitch$.subscribe((wallet) => {
      if (wallet['id'] === 'hot') {
        this.selectedWallet = 0;
      } else {
        this.selectedWallet = 1;
      }
      this._cdRef.detectChanges();
    });
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }

}
