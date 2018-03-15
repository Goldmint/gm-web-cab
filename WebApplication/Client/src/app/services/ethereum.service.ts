import { Injectable } from "@angular/core";
import { Observable } from "rxjs/Observable";
import { interval } from "rxjs/observable/interval";
import * as Web3 from "web3";
import { BehaviorSubject } from "rxjs/BehaviorSubject";
import { UserService } from "./user.service";
import { BigNumber } from 'bignumber.js'

// main contract
const EthFiatContractAddress = '0x0a750207851c11467aA48be72A749082fa7aA73D';
const EthFiatContractABI = '[{"constant":true,"inputs":[],"name":"creator","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_address","type":"address"}],"name":"setEthDepositAddress","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_requestId","type":"uint256"}],"name":"depositEth","outputs":[],"payable":true,"stateMutability":"payable","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"}],"name":"getGoldTransactionsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_newFiatFee","type":"address"}],"name":"changeFiatFeeContract","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"x","type":"bytes32"},{"name":"y","type":"bytes32"}],"name":"bytes64ToString","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"}],"name":"getUserHotGoldBalance","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_ipfsDocLink","type":"string"}],"name":"addDoc","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"fiatFee","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"}],"name":"getUserFiatBalance","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_index","type":"uint256"}],"name":"getDoc","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_userId","type":"string"},{"name":"_requestHash","type":"string"}],"name":"addBuyTokensRequest","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_index","type":"uint256"}],"name":"cancelRequest","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"getRequestsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_newController","type":"address"}],"name":"changeController","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"getHotWalletAddress","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_userId","type":"string"},{"name":"_requestHash","type":"string"}],"name":"addSellTokensRequest","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"getAllGoldTransactionsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"getDocCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_to","type":"address"}],"name":"changeCreator","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"x","type":"bytes32"}],"name":"bytes32ToString","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"goldToken","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_userId","type":"string"},{"name":"_amount","type":"int256"}],"name":"addGoldTransaction","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_to","type":"address"},{"name":"_value","type":"uint256"},{"name":"_userId","type":"string"}],"name":"transferGoldFromHotWallet","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"}],"name":"getFiatTransactionsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"},{"name":"_index","type":"uint256"}],"name":"getFiatTransaction","outputs":[{"name":"","type":"int256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_userId","type":"string"},{"name":"_isBuy","type":"bool"},{"name":"_amountCents","type":"uint256"},{"name":"_centsPerGold","type":"uint256"}],"name":"processInternalRequest","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"stor","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"ethDepositAddress","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_index","type":"uint256"}],"name":"getRequest","outputs":[{"name":"","type":"address"},{"name":"","type":"string"},{"name":"","type":"string"},{"name":"","type":"bool"},{"name":"","type":"uint8"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"mntpToken","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"s","type":"string"}],"name":"stringToBytes32","outputs":[{"name":"","type":"bytes32"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"getAllFiatTransactionsCount","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_index","type":"uint256"},{"name":"_amountCents","type":"uint256"},{"name":"_centsPerGold","type":"uint256"}],"name":"processRequest","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"getEthDepositAddress","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_userId","type":"string"},{"name":"_amountCents","type":"int256"}],"name":"addFiatTransaction","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"s","type":"string"}],"name":"stringToBytes64","outputs":[{"name":"","type":"bytes32"},{"name":"","type":"bytes32"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_userId","type":"string"},{"name":"_index","type":"uint256"}],"name":"getGoldTransaction","outputs":[{"name":"","type":"int256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_hotWalletAddress","type":"address"}],"name":"setHotWalletAddress","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"inputs":[{"name":"_mntpContractAddress","type":"address"},{"name":"_goldContractAddress","type":"address"},{"name":"_storageAddress","type":"address"},{"name":"_fiatFeeContract","type":"address"}],"payable":false,"stateMutability":"nonpayable","type":"constructor"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_from","type":"address"},{"indexed":true,"name":"_userId","type":"string"}],"name":"NewTokenBuyRequest","type":"event"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_from","type":"address"},{"indexed":true,"name":"_userId","type":"string"}],"name":"NewTokenSellRequest","type":"event"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_reqId","type":"uint256"}],"name":"RequestCancelled","type":"event"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_reqId","type":"uint256"}],"name":"RequestProcessed","type":"event"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_requestId","type":"uint256"},{"indexed":true,"name":"_address","type":"address"},{"indexed":false,"name":"_ethValue","type":"uint256"}],"name":"EthDeposited","type":"event"}]';

// gold token
const EthGoldContractAddress = '0x684184EeB977C94F8c362b6ffBC11164C6a283eC';
const EthGoldContractABI = '[{"constant":true,"inputs":[],"name":"creator","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"name","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_to","type":"address"}],"name":"rescueAllRewards","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_spender","type":"address"},{"name":"_value","type":"uint256"}],"name":"approve","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_who","type":"address"},{"name":"_tokens","type":"uint256"}],"name":"burnTokens","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"totalSupply","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[],"name":"startMigration","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_goldFeeAddress","type":"address"}],"name":"setGoldFeeAddress","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_lock","type":"bool"}],"name":"lockTransfer","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_from","type":"address"},{"name":"_to","type":"address"},{"name":"_value","type":"uint256"}],"name":"transferFrom","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"","type":"address"}],"name":"balances","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_migrationAddress","type":"address"}],"name":"setMigrationContractAddress","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"decimals","outputs":[{"name":"","type":"uint8"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"contractLocked","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_address","type":"address"}],"name":"setCreator","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"migrationFinished","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_who","type":"address"},{"name":"_tokens","type":"uint256"}],"name":"issueTokens","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"_owner","type":"address"}],"name":"balanceOf","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"migrationAddress","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_to","type":"address"}],"name":"changeCreator","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"storageControllerAddress","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"migrationStarted","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"goldFee","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"transfersLocked","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[],"name":"finishMigration","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_contractLocked","type":"bool"}],"name":"lockContract","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"getTotalIssued","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"goldmintTeamAddress","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_address","type":"address"}],"name":"setStorageControllerContractAddress","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"symbol","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"totalBurnt","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"getTotalBurnt","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_teamAddress","type":"address"}],"name":"setGoldmintTeamAddress","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_to","type":"address"},{"name":"_value","type":"uint256"}],"name":"transferRewardWithoutFee","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_to","type":"address"},{"name":"_value","type":"uint256"}],"name":"transfer","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"mntpToken","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_owner","type":"address"},{"name":"_spender","type":"address"}],"name":"allowance","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"totalIssued","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"inputs":[{"name":"_mntpContractAddress","type":"address"},{"name":"_goldmintTeamAddress","type":"address"},{"name":"_goldFeeAddress","type":"address"}],"payable":false,"stateMutability":"nonpayable","type":"constructor"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_from","type":"address"},{"indexed":true,"name":"_to","type":"address"},{"indexed":false,"name":"_value","type":"uint256"}],"name":"Transfer","type":"event"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_owner","type":"address"},{"indexed":true,"name":"_spender","type":"address"},{"indexed":false,"name":"_value","type":"uint256"}],"name":"Approval","type":"event"}]';

// mntp token
const EthMntpContractAddress = '0xbA363e44E66d94909B211999507CAa351D6e01AA';
const EthMntpContractABI = '[{"constant":true,"inputs":[],"name":"creator","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"name","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_spender","type":"address"},{"name":"_value","type":"uint256"}],"name":"approve","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_who","type":"address"},{"name":"_tokens","type":"uint256"}],"name":"burnTokens","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_icoContractAddress","type":"address"}],"name":"setIcoContractAddress","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"totalSupply","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_lock","type":"bool"}],"name":"lockTransfer","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_from","type":"address"},{"name":"_to","type":"address"},{"name":"_value","type":"uint256"}],"name":"transferFrom","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"decimals","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_creator","type":"address"}],"name":"setCreator","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":false,"inputs":[{"name":"_who","type":"address"},{"name":"_tokens","type":"uint256"}],"name":"issueTokens","outputs":[],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[{"name":"_owner","type":"address"}],"name":"balanceOf","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"lockTransfers","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"symbol","outputs":[{"name":"","type":"string"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[],"name":"icoContractAddress","outputs":[{"name":"","type":"address"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":false,"inputs":[{"name":"_to","type":"address"},{"name":"_value","type":"uint256"}],"name":"transfer","outputs":[{"name":"","type":"bool"}],"payable":false,"stateMutability":"nonpayable","type":"function"},{"constant":true,"inputs":[],"name":"TOTAL_TOKEN_SUPPLY","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"constant":true,"inputs":[{"name":"_owner","type":"address"},{"name":"_spender","type":"address"}],"name":"allowance","outputs":[{"name":"","type":"uint256"}],"payable":false,"stateMutability":"view","type":"function"},{"inputs":[],"payable":false,"stateMutability":"nonpayable","type":"constructor"},{"payable":false,"stateMutability":"nonpayable","type":"fallback"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_from","type":"address"},{"indexed":true,"name":"_to","type":"address"},{"indexed":false,"name":"_value","type":"uint256"}],"name":"Transfer","type":"event"},{"anonymous":false,"inputs":[{"indexed":true,"name":"_owner","type":"address"},{"indexed":true,"name":"_spender","type":"address"},{"indexed":false,"name":"_value","type":"uint256"}],"name":"Approval","type":"event"}]';

@Injectable()
export class EthereumService {

  private _web3Infura: Web3 = null;
  private _web3Metamask: Web3 = null;
  private _lastAddress: string | null;
  private _userId: string | null;

  private _contractFiatInfura: any;
  private _contractFiatMetamask: any;
  public _contractGold: any;
  public _contractHotGold: any;
  private _contractMntp: any;
  private _totalGoldBalances = {issued: null, burnt: null};
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
  private _obsTotalGoldBalancesSubject = new BehaviorSubject<Object>(null);
  private _obsTotalGoldBalances: Observable<Object> = this._obsTotalGoldBalancesSubject.asObservable();

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
      this._web3Infura = new Web3(new Web3.providers.HttpProvider('https://rinkeby.infura.io/ErpvjHXqcahfBLahHPfh'));

        if (this._web3Infura.eth) {
          this._contractFiatInfura = this._web3Infura.eth.contract(JSON.parse(EthFiatContractABI)).at(EthFiatContractAddress);
          this._contractHotGold = this._web3Infura.eth.contract(JSON.parse(EthGoldContractABI)).at(EthGoldContractAddress);
        } else {
          this._web3Infura = null;
        }
    }

    if (!this._web3Metamask && window.hasOwnProperty('web3')) {
      this._web3Metamask = new Web3(window['web3'].currentProvider);

      if (this._web3Metamask.eth) {
        this._contractFiatMetamask = this._web3Metamask.eth.contract(JSON.parse(EthFiatContractABI)).at(EthFiatContractAddress);
        this._contractGold = this._web3Metamask.eth.contract(JSON.parse(EthGoldContractABI)).at(EthGoldContractAddress);
        this._contractMntp = this._web3Metamask.eth.contract(JSON.parse(EthMntpContractABI)).at(EthMntpContractAddress);
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

  private updateTotalGoldBalances() {
    if (this._contractHotGold) {
      this._contractHotGold.getTotalBurnt((err, res) => {
        if (!this._totalGoldBalances.burnt || !this._totalGoldBalances.burnt.eq(res)) {
          this._totalGoldBalances.burnt = res;
          this._totalGoldBalances.issued && this._obsTotalGoldBalancesSubject.next(this._totalGoldBalances);
        }
      });
      this._contractHotGold.getTotalIssued((err, res) => {
        if (!this._totalGoldBalances.issued || !this._totalGoldBalances.issued.eq(res)) {
          this._totalGoldBalances.issued = res;
          this._totalGoldBalances.burnt && this._obsTotalGoldBalancesSubject.next(this._totalGoldBalances);
        }
      });
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
    this.updateTotalGoldBalances();
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

  public getObservableTotalGoldBalances(): Observable<Object> {
    return this._obsTotalGoldBalances;
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

  public transferGoldToWallet(fromAddr: string, toAddr: string, goldAmount: BigNumber) {
    if (this._contractGold == null) return;
    var goldAmountStr = goldAmount.times(new BigNumber(10).pow(18)).decimalPlaces(0, BigNumber.ROUND_DOWN).toString();
    this._contractGold.transfer.sendTransaction(toAddr, goldAmountStr, { from: fromAddr, value: 0 }, (err, res) => { });
  }
}
