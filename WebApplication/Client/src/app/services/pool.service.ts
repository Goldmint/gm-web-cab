import { Injectable } from '@angular/core';
import {BehaviorSubject} from "rxjs/BehaviorSubject";
import {Observable} from "rxjs/Observable";
import {EthereumService} from "./index";
import {BigNumber} from "bignumber.js";
import {Subject} from "rxjs/Subject";
import {MessageBoxService} from "./message-box.service";
import {environment} from "../../environments/environment";
import {interval} from "rxjs/observable/interval";
import * as Web3 from "web3";

@Injectable()
export class PoolService {

  private _obsUserStakeSubject = new BehaviorSubject<any>(null);
  private _obsUserStake: Observable<any> = this._obsUserStakeSubject.asObservable();

  private _obsUserFrozenStakeSubject = new BehaviorSubject<any>(null);
  private _obsUserFrozenStake: Observable<any> = this._obsUserFrozenStakeSubject.asObservable();

  private _obsMntpTokenUserRewardSubject = new BehaviorSubject<any>(null);
  private _obsMntpTokenUserReward: Observable<any> = this._obsMntpTokenUserRewardSubject.asObservable();

  private _obsGoldTokenUserRewardSubject = new BehaviorSubject<any>(null);
  private _obsGoldTokenUserReward: Observable<any> = this._obsGoldTokenUserRewardSubject.asObservable();

  public getSuccessHoldRequestLink$ = new Subject();
  public getSuccessWithdrawRequestLink$ = new Subject();
  public getSuccessUnholdRequestLink$ = new Subject();
  public getSuccessMNTTokenLink$ = new Subject();

  public destroy$ = new Subject();

  private etherscanUrl = environment.etherscanUrl;
  private metamaskNetwork: any = null;
  private Web3 = new Web3();

  constructor(
    private _ethService: EthereumService,
    private _messageBox: MessageBoxService
  ) {
    this._ethService.getObservableEthAddress().subscribe(address => {
      if (address) {
        this.getPoolData();
      }
    });

    this._ethService.getObservableNetwork().subscribe(network => {
      if (network && network !== this.metamaskNetwork) {
        this.metamaskNetwork && this.getPoolData();
        this.metamaskNetwork = network;
      }
    });
  }

  public getPoolData() {
    this.getUserStake();
    this.getUserFrozenStake();
    this.getMntpTokenUserReward();
    // this.getGoldTokenUserReward();
  }

  public updatePoolData() {
    interval(15000).takeUntil(this.destroy$).subscribe(this.getPoolData.bind(this));
  }

  private getUserStake() {
    if (!this._ethService.poolContract) {
      this._obsUserStakeSubject.next(null);
    } else {
      this._ethService.poolContract.getUserStake((err, res) => {
        this._obsUserStakeSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
      });
    }
  }

  private getUserFrozenStake() {
    if (!this._ethService.poolContract) {
      this._obsUserFrozenStakeSubject.next(null);
    } else {
      this._ethService.poolContract.getUserFrozenStake((err, res) => {
        this._obsUserFrozenStakeSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
      });
    }
  }

  private getMntpTokenUserReward() {
    if (!this._ethService.poolContract) {
      this._obsMntpTokenUserRewardSubject.next(null);
    } else {
      this._ethService.poolContract.getMntpTokenUserReward((err, res) => {
        this._obsMntpTokenUserRewardSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
      });
    }
  }

  private getGoldTokenUserReward() {
    if (!this._ethService.poolContract) {
      this._obsGoldTokenUserRewardSubject.next(null);
    } else {
      this._ethService.poolContract.getGoldTokenUserReward((err, res) => {
        this._obsGoldTokenUserRewardSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
      });
    }
  }

  public getObsUserStake(): Observable<string> {
    return this._obsUserStake;
  }

  public getObsUserFrozenStake(): Observable<string> {
    return this._obsUserFrozenStake;
  }

  public getObsMntpTokenUserReward(): Observable<string> {
    return this._obsMntpTokenUserReward;
  }

  public getObsGoldTokenUserReward(): Observable<string> {
    return this._obsGoldTokenUserReward;
  }

  public successTransactionModal(hash: any, phrases: any) {
    this._messageBox.alert(`
      <div class="text-center">
        <div class="font-weight-500 mb-2">${phrases.Heading}</div>
        <div>${phrases.Hash}</div>
        <div class="mb-2 success-tx-hash">${hash}</div>
        <a href="${this.etherscanUrl}${hash}" target="_blank">${phrases.Link}</a>
      </div>
    `).subscribe();
  }

  public holdStake(fromAddr: string, amount: number, gasPrice: number) {
    if (!this._ethService.poolContract || !this._ethService.contractMntp) return

    const wei = this.Web3.toWei(amount);
    this._ethService.contractMntp.allowance(fromAddr, this._ethService.EthPoolContractAddress, (err, res) => {
      if (res) {
        const allowance = +new BigNumber(res.toString()).div(new BigNumber(10).pow(18));

        if (allowance !== 0 && allowance !== amount) {
          this._ethService.contractMntp.approve(this._ethService.EthPoolContractAddress, 0, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
            res && setTimeout(() => {
              this._holdStake(fromAddr, wei, gasPrice);
            }, 1000);
          });
        } else {
          this._holdStake(fromAddr, wei, gasPrice);
        }
      }
    });
  }

  private _holdStake(fromAddr: string, wei: string, gasPrice: number) {
    this._ethService.contractMntp.approve(this._ethService.EthPoolContractAddress, wei, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      res && setTimeout(() => {
        this._ethService.poolContract.holdStake(wei, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
          this.getSuccessHoldRequestLink$.next(res);
        });
      }, 1000);
    });
  }

  public withdrawUserReward(fromAddr: string, gasPrice: number) {
    if (!this._ethService.poolContract) return

    this._ethService.poolContract.withdrawUserReward({ from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      this.getSuccessWithdrawRequestLink$.next(res);
    });
  }

  public withdrawRewardAndUnholdStake(fromAddr: string, gasPrice: number) {
    if (!this._ethService.poolContract) return

    this._ethService.poolContract.withdrawRewardAndUnholdStake({ from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      this.getSuccessUnholdRequestLink$.next(res);
    });
  }

  public freezeStake(sumusAddress, fromAddr: string, gasPrice: number) {
    if (!this._ethService.poolContract) return

    this._ethService.poolContract.freezeStake(sumusAddress, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      this.getSuccessMNTTokenLink$.next(res);
    });
  }

}
