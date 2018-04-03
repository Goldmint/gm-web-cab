import { Injectable } from "@angular/core";
import { Observable } from "rxjs/Observable";
import { interval } from "rxjs/observable/interval";
import * as Web3 from "web3";
import { BehaviorSubject } from "rxjs/BehaviorSubject";
import { UserService } from "./user.service";
import { BigNumber } from 'bignumber.js'
import {environment} from "../../environments/environment";

@Injectable()
export class EthereumService {
  private _infuraUrl = environment.infuraUrl;
  // main contract
  private EthFiatContractAddress = environment.EthFiatContractAddress;
  private EthFiatContractABI = environment.EthFiatContractABI;
  // gold token
  private EthGoldContractAddress = environment.EthGoldContractAddress;
  private EthGoldContractABI = environment.EthGoldContractABI;
  // mntp token
  private EthMntpContractAddress = environment.EthMntpContractAddress;
  private EthMntpContractABI = environment.EthMntpContractABI;

  private _web3Infura: Web3 = null;
  private _web3Metamask: Web3 = null;
  private _lastAddress: string | null;
  private _userId: string | null;

  private _contractFiatInfura: any;
  private _contractFiatMetamask: any;
  public _contractGold: any;
  public _contractHotGold: any;
  private _contractMntp: any;
  private _contactsInitted: boolean = false;
  private _currentUsdBalance: number = null;

  private _obsEthAddressSubject = new BehaviorSubject<string>(null);
  private _obsEthAddress: Observable<string> = this._obsEthAddressSubject.asObservable();
  private _obsGoldBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsGoldBalance: Observable<BigNumber> = this._obsGoldBalanceSubject.asObservable();
  private _obsUsdBalanceSubject = new BehaviorSubject<number>(null);
  private _obsUsdBalance: Observable<number> = this._obsUsdBalanceSubject.asObservable();
  private _obsMntpBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsMntpBalance: Observable<BigNumber> = this._obsMntpBalanceSubject.asObservable();
  private _obsHotGoldBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsHotGoldBalance: Observable<BigNumber> = this._obsHotGoldBalanceSubject.asObservable();

  constructor(
    private _userService: UserService
  ) {
    console.log('EthereumService constructor');

    this._userService.currentUser.subscribe(currentUser => {
      this._userId = currentUser != null && currentUser.id ? currentUser.id : null;
    });

    interval(500).subscribe(this.checkWeb3.bind(this));

    interval(7500).subscribe(this.checkBalance.bind(this));
  }

  private checkWeb3() {

    if (!this._web3Infura) {
      this._web3Infura = new Web3(new Web3.providers.HttpProvider(this._infuraUrl));

        if (this._web3Infura.eth) {
          this._contractFiatInfura = this._web3Infura.eth.contract(JSON.parse(this.EthFiatContractABI)).at(this.EthFiatContractAddress);
          this._contractHotGold = this._web3Infura.eth.contract(JSON.parse(this.EthGoldContractABI)).at(this.EthGoldContractAddress);
        } else {
          this._web3Infura = null;
        }
    }

    if (!this._web3Metamask && window.hasOwnProperty('web3')) {
      this._web3Metamask = new Web3(window['web3'].currentProvider);

      if (this._web3Metamask.eth) {
        this._contractFiatMetamask = this._web3Metamask.eth.contract(JSON.parse(this.EthFiatContractABI)).at(this.EthFiatContractAddress);
        this._contractGold = this._web3Metamask.eth.contract(JSON.parse(this.EthGoldContractABI)).at(this.EthGoldContractAddress);
        this._contractMntp = this._web3Metamask.eth.contract(JSON.parse(this.EthMntpContractABI)).at(this.EthMntpContractAddress);
      } else {
        this._web3Metamask = null;
      }
    }

    if (!this._contactsInitted && this._userId) {
      this._contactsInitted = true;
      this.checkBalance();
    }

    var addr = this._web3Metamask && this._web3Metamask.eth && this._web3Metamask.eth.accounts.length
      ? this._web3Metamask.eth.accounts[0] : null;
    if (this._lastAddress != addr) {
      this._lastAddress = addr;
      console.log("EthereumService: new eth address (MM): " + addr);
      this.emitAddress(addr);
    }
  }

  private checkBalance() {
    if (this._lastAddress != null) {
      // check via eth
      this.updateGoldBalance(this._lastAddress);
      this.updateMntpBalance(this._lastAddress);
    }

    this.updateUsdBalance();
    this.checkHotBalance();
  }

  private checkHotBalance() {
    this._userId != null && this._contractFiatInfura && this._contractFiatInfura.getUserHotGoldBalance(this._userId, (err, res) => {
      this._obsHotGoldBalanceSubject.next(res.div(new BigNumber(10).pow(18)));
    });
  }

  private emitAddress(ethAddress: string) {
    this._obsEthAddressSubject.next(ethAddress);
    this._obsGoldBalanceSubject.next(null);
    this._obsMntpBalanceSubject.next(null);
    this.checkBalance();
  }

  private updateUsdBalance() {
    if (this._userId == null || this._contractFiatInfura == null) {
      this._obsUsdBalanceSubject.next(null);
    } else {
      this._contractFiatInfura.getUserFiatBalance(this._userId, (err, res) => {
        let balance = res.toString() / 100;
        balance !== this._currentUsdBalance && this._obsUsdBalanceSubject.next(this._currentUsdBalance = balance);
      });
    }
  }

  private updateGoldBalance(addr: string) {
    if (addr == null || this._contractGold == null) {
      this._obsGoldBalanceSubject.next(null);
    } else {
      this._contractGold.balanceOf(addr, (err, res) => {
        this._obsGoldBalanceSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
      });
    }
  }

  private updateMntpBalance(addr: string) {
    if (addr == null || this._contractGold == null) {
      this._obsMntpBalanceSubject.next(null);
    } else {
      this._contractMntp.balanceOf(addr, (err, res) => {
        this._obsMntpBalanceSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
      });
    }
  }

  // ---

  public isValidAddress(addr: string): boolean {
    return (new Web3()).isAddress(addr);
  }

  public getEthAddress(): string | null {
    return this._obsEthAddressSubject.value;
  }

  public getObservableEthAddress(): Observable<string> {
    return this._obsEthAddress;
  }

  public getObservableUsdBalance(): Observable<number> {
    return this._obsUsdBalance;
  }

  public getObservableGoldBalance(): Observable<BigNumber> {
    return this._obsGoldBalance;
  }

  public getObservableHotGoldBalance(): Observable<BigNumber> {
    return this._obsHotGoldBalance;
  }

  public getObservableMntpBalance(): Observable<BigNumber> {
    return this._obsMntpBalance;
  }

  // ---

  public sendBuyRequest(fromAddr: string, payload: any[]) {
    if (this._contractFiatMetamask == null) return;
    this._contractFiatMetamask.addBuyTokensRequest.sendTransaction(payload[0], payload[1], { from: fromAddr, value: 0 }, (err, res) => { });
  }

  public sendSellRequest(fromAddr: string, payload: any[]) {
    if (this._contractFiatMetamask == null) return;
    this._contractFiatMetamask.addSellTokensRequest.sendTransaction(payload[0], payload[1], { from: fromAddr, value: 0 }, (err, res) => { });
  }

  public ethDepositRequest(fromAddr: string, requestId: number, amount: BigNumber) {
    if (this._contractFiatMetamask == null) return;
    const wei = new BigNumber(amount).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
    this._contractFiatMetamask.depositEth.sendTransaction(requestId, { from: fromAddr, value: wei.toString() }, (err, res) => { });
  }

  public transferGoldToWallet(fromAddr: string, toAddr: string, goldAmount: BigNumber) {
    if (this._contractGold == null) return;
    var goldAmountStr = goldAmount.times(new BigNumber(10).pow(18)).decimalPlaces(0, BigNumber.ROUND_DOWN).toString();
    this._contractGold.transfer.sendTransaction(toAddr, goldAmountStr, { from: fromAddr, value: 0 }, (err, res) => { });
  }
}
