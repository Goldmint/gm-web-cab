import {ChangeDetectionStrategy, ChangeDetectorRef, Component, HostBinding, OnInit} from '@angular/core';
import {environment} from "../../../environments/environment";
import {PoolService} from "../../services/pool.service";
import {Subject} from "rxjs/Subject";
import {EthereumService, MessageBoxService, UserService} from "../../services";
import {TranslateService} from "@ngx-translate/core";
import {Subscription} from "rxjs/Subscription";
import {CommonService} from "../../services/common.service";

@Component({
  selector: 'app-blockchain-pool-page',
  templateUrl: './blockchain-pool-page.component.html',
  styleUrls: ['./blockchain-pool-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BlockchainPoolPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  public isProduction = environment.isProduction;
  public heldTokens: number = 0;
  public mntpReward: number = 0;
  public goldReward: number = 0;
  public userFrozenStake: number = null;

  public heldTokensOld: number = 0;
  public mntpRewardOld: number = 0;
  public goldRewardOld: number = 0;

  public ethAddress: string = null;
  public loading: boolean = false;
  public isAuthenticated: boolean = false;
  public submitMethod = ['withdrawUserReward', 'withdrawRewardAndUnholdStake', 'withdrawRewardAndUnholdStakeOld'];
  public MMNetwork = environment.MMNetwork;
  public isInvalidNetwork: boolean = true;
  public etherscanContractUrl = environment.etherscanContractUrl;
  public poolContractAddress = environment.EthPoolContractAddress;

  private timeoutPopUp;
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private sub1: Subscription;

  constructor(
    private _ethService: EthereumService,
    private _poolService: PoolService,
    private _commonService: CommonService,
    private _cdRef: ChangeDetectorRef,
    private _userService: UserService,
    private _translate: TranslateService,
    private _messageBox: MessageBoxService
  ) { }

  ngOnInit() {
    this._poolService.getPoolData();

    this.isAuthenticated = this._userService.isAuthenticated();

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(address => {
      if (!this.ethAddress && address) {
        this._messageBox.closeModal();
      }
      this.ethAddress = address;
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

    this.initWithdrawTransactionModal();
    this.initUnholdTransactionModal();

    if (window.hasOwnProperty('web3') || window.hasOwnProperty('ethereum')) {
      this.loading = true;
      this.timeoutPopUp = setTimeout(() => {
        this._ethService.connectToMetaMask();
        !this.ethAddress && this._userService.showLoginToMMBox('HeadingPool');
        this.loading = false;
        this._cdRef.markForCheck();
      }, 4000);
    }

    this._poolService.getObsUserStake().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null) {
        this.heldTokens = +data;
        this.loading = false;
        this._cdRef.markForCheck();
      }
    });

    this._poolService.getObsMntpTokenUserReward().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null) {
        this.mntpReward = +data;
        this._cdRef.markForCheck();
      }
    });

    this._poolService.getObsGoldTokenUserReward().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null) {
        this.goldReward = +data;
        this._cdRef.markForCheck();
      }
    });

    this._poolService.getObsOldUserStake().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null) {
        this.heldTokensOld = +data;
        this._cdRef.markForCheck();
      }
    });

    this._poolService.getObsOldMntpTokenUserReward().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null) {
        this.mntpRewardOld = +data;
        this._cdRef.markForCheck();
      }
    });

    this._poolService.getObsOldGoldTokenUserReward().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null) {
        this.goldRewardOld = +data;
        this._cdRef.markForCheck();
      }
    });

    this._poolService.getObsUserFrozenStake().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null) {
        this.userFrozenStake = +data;
        this._cdRef.markForCheck();
      }
    });
  }

  setPoolDataUpdate() {
    this._poolService.destroy$.next(true);
    this._poolService.updatePoolData();
  }

  initWithdrawTransactionModal() {
    this._poolService.getSuccessWithdrawRequestLink$.takeUntil(this.destroy$).subscribe(hash => {
      if (hash) {
        this._translate.get('MessageBox.SuccessTransactionModal').subscribe(phrases => {
          this._poolService.successTransactionModal(hash, phrases);
          this.setPoolDataUpdate();
        });
      }
    });
  }

  initUnholdTransactionModal() {
    this._poolService.getSuccessUnholdRequestLink$.takeUntil(this.destroy$).subscribe(hash => {
      if (hash) {
        this._translate.get('MessageBox.SuccessTransactionModal').subscribe(phrases => {
          this._poolService.successTransactionModal(hash, phrases);
          this.setPoolDataUpdate();
        });
      }
    });
  }

  onSubmit(method: string) {
    if (!this.isAuthenticated) {
      this._messageBox.authModal();
      return;
    }

    let firstLoad = true;
    this.sub1 && this.sub1.unsubscribe();
    this.sub1 = this._ethService.getObservableGasPrice().takeUntil(this.destroy$).subscribe((price) => {
      if (price && firstLoad) {
        firstLoad = false;
        this._poolService[method](this.ethAddress, +price * Math.pow(10, 9));
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    this._poolService.destroy$.next(true);
    clearTimeout(this.timeoutPopUp);
  }

}
