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

  public heldTokens: number = 0;
  public mntpReward: number = 0;
  public goldReward: number = 0;
  public userFrozenStake: number = null;

  public ethAddress: string = null;
  public loading: boolean = true;
  public submitMethod = ['withdrawUserReward', 'withdrawRewardAndUnholdStake'];
  public MMNetwork = environment.MMNetwork;
  public isInvalidNetwork: boolean = true;
  public etherscanContractUrl = environment.etherscanContractUrl;
  public poolContractAddress = environment.EthPoolContractAddress;
  public noMetamask: boolean = false;

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
    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(address => {
      if (!this.ethAddress && address) {
        this._messageBox.closeModal();
      }
      this.ethAddress = address;
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableNetwork().takeUntil(this.destroy$).subscribe(network => {
      if (network !== null) {
        this.isInvalidNetwork = network != this.MMNetwork.index;
        this._cdRef.markForCheck();
      }
    });

    this.detectMetaMask();
    this.initWithdrawTransactionModal();
    this.initUnholdTransactionModal();

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

    this._poolService.getObsUserFrozenStake().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null) {
        this.userFrozenStake = +data;
        this._cdRef.markForCheck();
      }
    });
  }

  detectMetaMask() {
    if (!window.hasOwnProperty('web3') && !window.hasOwnProperty('ethereum')) {
      this.noMetamask = true;
      this.loading = false;
      this._cdRef.markForCheck();
    } else {
      setTimeout(() => {
        this.loading = false;
        this._cdRef.markForCheck();
      }, 3000);
    }
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
    if (this.noMetamask) {
      this._userService.showGetMetamaskModal();
      return;
    }

    if (!this.ethAddress) {
      this._ethService.connectToMetaMask();
      this._userService.showLoginToMMBox('HeadingPool');
      return;
    }

    if (this.isInvalidNetwork) {
      this._userService.invalidNetworkModal(this.MMNetwork.name);
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
