import { Injectable } from "@angular/core";
import { Observable } from "rxjs/Observable";
import { interval } from "rxjs/observable/interval";

import * as Web3 from "web3";
import { BehaviorSubject } from "rxjs/BehaviorSubject";

const EthContractAddress = '0xC31723C9a4B480Fe6Fe81bf8f80Ca65F15796827';
const EthContractABI = '[{"constant":true,"inputs":[],"name":"creator","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"}],"name":"getGoldTransactionsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_newFiatFee","type":"address"}],"name":"changeFiatFeeContract","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"x","type":"bytes32"},{"name":"y","type":"bytes32"}],"name":"bytes64ToString","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_ipfsDocLink","type":"string"}],"name":"addDoc","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"fiatFee","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"}],"name":"getUserFiatBalance","outputs":[{"name":"","type":"int256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_index","type":"uint256"}],"name":"getDoc","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_userId","type":"string"},{"name":"_requestHash","type":"string"}],"name":"addBuyTokensRequest","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_index","type":"uint256"}],"name":"cancelRequest","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"getRequestsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"}],"name":"getUserGoldBalance","outputs":[{"name":"","type":"int256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_newController","type":"address"}],"name":"changeController","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_userId","type":"string"},{"name":"_requestHash","type":"string"}],"name":"addSellTokensRequest","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"getAllGoldTransactionsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"getDocCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_to","type":"address"}],"name":"changeCreator","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"x","type":"bytes32"}],"name":"bytes32ToString","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"goldToken","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_userId","type":"string"},{"name":"_amount","type":"int256"}],"name":"addGoldTransaction","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"}],"name":"getFiatTransactionsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"},{"name":"_index","type":"uint256"}],"name":"getFiatTransaction","outputs":[{"name":"","type":"int256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"stor","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_index","type":"uint256"}],"name":"getRequest","outputs":[{"name":"","type":"address"},{"name":"","type":"string"},{"name":"","type":"string"},{"name":"","type":"bool"},{"name":"","type":"uint8"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"mntpToken","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"s","type":"string"}],"name":"stringToBytes32","outputs":[{"name":"","type":"bytes32"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"getAllFiatTransactionsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_index","type":"uint256"},{"name":"_amountCents","type":"uint256"},{"name":"_centsPerGold","type":"uint256"}],"name":"processRequest","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_userId","type":"string"},{"name":"_amountCents","type":"int256"}],"name":"addFiatTransaction","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"s","type":"string"}],"name":"stringToBytes64","outputs":[{"name":"","type":"bytes32"},{"name":"","type":"bytes32"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"},{"name":"_index","type":"uint256"}],"name":"getGoldTransaction","outputs":[{"name":"","type":"int256"}],"payable":false,"stateMutability":"view","type":"function"},{"inputs":[{"name":"_mntpContractAddress","type":"address"},{"name":"_goldContractAddress","type":"address"},{"name":"_storageAddress","type":"address"},{"name":"_fiatFeeContract","type":"address"}],"payable":false,"stateMutability":"nonpayable","type":"constructor"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_from","type":"address"},{"indexed":true,"name":"_userId","type":"string"}],"name":"NewTokenBuyRequest","type":"event"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_from","type":"address"},{"indexed":true,"name":"_userId","type":"string"}],"name":"NewTokenSellRequest","type":"event"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_reqId","type":"uint256"}],"name":"RequestCancelled","type":"event"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_reqId","type":"uint256"}],"name":"RequestProcessed","type":"event"}]';

@Injectable()
export class EthereumService {

  private _web3: Web3;
  private _contract: any;
  private _obsEthAddressSubject = new BehaviorSubject<string | null>(null);
  private _observableEthAddress: Observable<string> = this._obsEthAddressSubject.asObservable();

  constructor(
  ) {
    console.log('EthereumService constructor');

    interval(1500)
      .subscribe(time => {
        this.checkWeb3();
      })
      ;
  }

  checkWeb3() {
    if (window.hasOwnProperty('web3') && !this._web3) {
      this._web3 = new Web3(window['web3'].currentProvider);
      if (this._web3.eth) {
        this._contract = this._web3.eth.contract(JSON.parse(EthContractABI)).at(EthContractAddress);
      } else {
        this._web3 = null;
      }
    }
    this.emitAddress(this.getEthAddress());
  }

  emitAddress(ethAddress:string) {
    this._obsEthAddressSubject.next(ethAddress);
  }

  // ---

  public getEthAddress(): string {
    return this._web3 && this._web3.eth && this._web3.eth.accounts.length ? this._web3.eth.accounts[0] : null;
  }

  public getObservableEthAddress(): Observable<string> {
    return this._observableEthAddress;
  }

  // ---

  public sendBuyRequest(fromAddr:string, payload:any[]) {
    if (this._contract == null) return;
    this._contract.addBuyTokensRequest.sendTransaction(payload[0], payload[1], { from: fromAddr, value: 0 }, (err, res) => { });
  }

  public sendSellRequest(fromAddr: string, payload: any[]) {
    if (this._contract == null) return;
    this._contract.addSellTokensRequest.sendTransaction(payload[0], payload[1], { from: fromAddr, value: 0 }, (err, res) => { });
  }
}
