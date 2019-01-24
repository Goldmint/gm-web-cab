import { Injectable } from "@angular/core";
import { Observable } from "rxjs/Observable";
import { interval } from "rxjs/observable/interval";
import * as Web3 from "web3";
import { BehaviorSubject } from "rxjs/BehaviorSubject";
import { UserService } from "./user.service";
import { BigNumber } from 'bignumber.js'
import {environment} from "../../environments/environment";
import {HttpClient} from "@angular/common/http";
import {Subject} from "rxjs/Subject";
import {NavigationEnd, Router} from "@angular/router";

@Injectable()
export class EthereumService {
  private _infuraUrl = environment.infuraUrl;
  private _etherscanGetABIUrl = environment.etherscanGetABIUrl;
  private _gasPriceLink = environment.gasPriceLink;
  // main contract
  private EthContractAddress = environment.EthContractAddress;
  private EthContractABI: string;
  // gold token
  private EthGoldContractAddress: string = environment.EthGoldContractAddress;
  private EthGoldContractABI: string;
  // mntp token
  private EthMntpContractAddress: string = environment.EthMntpContractAddress;
  private EthMntpContractABI: string;
  // pool contract
  public EthPoolContractAddress: string = environment.EthPoolContractAddress;
  private EthPoolContractABI: string;

  private _web3Infura: Web3 = null;
  private _web3Metamask: Web3 = null;
  private _lastAddress: string | null;
  private _userId: string | null;
  private _metamaskNetwork: number = null

  private _contractInfura: any;
  private _contractMetamask: any;

  public _contractGold: any;
  public _contractHotGold: any;
  public poolContract: any;
  public contractMntp: any;
  private _contactsInitted: boolean = false;
  private _totalGoldBalances = {issued: null, burnt: null};
  private _allowedUrlOccurrencesForInject = [
    'buy', 'sell', 'transfer', 'transparency', 'master-node', 'blockchain-pool'
  ];
  private checkWeb3Interval = null;

  private _obsEthAddressSubject = new BehaviorSubject<string>(null);
  private _obsEthAddress: Observable<string> = this._obsEthAddressSubject.asObservable();
  private _obsGoldBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsGoldBalance: Observable<BigNumber> = this._obsGoldBalanceSubject.asObservable();
  private _obsMntpBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsMntpBalance: Observable<BigNumber> = this._obsMntpBalanceSubject.asObservable();
  private _obsHotGoldBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsHotGoldBalance: Observable<BigNumber> = this._obsHotGoldBalanceSubject.asObservable();
  private _obsEthBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsEthBalance: Observable<BigNumber> = this._obsEthBalanceSubject.asObservable();
  private _obsEthLimitBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsEthLimitBalance: Observable<BigNumber> = this._obsEthLimitBalanceSubject.asObservable();
  private _obsTotalGoldBalancesSubject = new BehaviorSubject<Object>(null);
  private _obsTotalGoldBalances: Observable<Object> = this._obsTotalGoldBalancesSubject.asObservable();
  private _obsGasPriceSubject = new BehaviorSubject<Object>(null);
  private _obsGasPrice: Observable<Object> = this._obsGasPriceSubject.asObservable();
  private _obsNetworkSubject = new BehaviorSubject<Number>(null);
  private _obsNetwork: Observable<Number> = this._obsNetworkSubject.asObservable();

  public isPoolContractLoaded$ = new BehaviorSubject(null);

  public getSuccessBuyRequestLink$ = new Subject();
  public getSuccessSellRequestLink$ = new Subject();
  public getSuccessMigrationGoldLink$ = new Subject();
  public getSuccessMigrationMntpLink$ = new Subject();

  constructor(
    private _userService: UserService,
    private _http: HttpClient,
    private router: Router
  ) {
    this._userService.currentUser.subscribe(currentUser => {
      this._userId = currentUser != null && currentUser.id ? currentUser.id : null;
    });

    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd && !this.checkWeb3Interval) {
        this._allowedUrlOccurrencesForInject.forEach(url => {
          if (event.urlAfterRedirects.indexOf(url) >= 0) {
            (window.hasOwnProperty('web3') || window.hasOwnProperty('ethereum')) && this._obsHotGoldBalanceSubject.next(null);
            this.checkWeb3Interval = interval(500).subscribe(this.checkWeb3.bind(this));
            interval(7500).subscribe(this.checkBalance.bind(this));
          }
        });
        !this.checkWeb3Interval && this._obsHotGoldBalanceSubject.next(new BigNumber(0));
      }
    });
  }

  getContractABI(address) {
    return this._http.get(`${this._etherscanGetABIUrl}/api?module=contract&action=getabi&address=${address}&forma=raw`)
  }

  private checkWeb3() {

    if (!this._web3Infura) {
      this._web3Infura = new Web3(new Web3.providers.HttpProvider(this._infuraUrl));

      this.getContractABI(this.EthContractAddress).subscribe(abi => {
        this.EthContractABI = abi['result'];

        if (this._web3Infura.eth) {
          this._contractInfura = this._web3Infura.eth.contract(JSON.parse(this.EthContractABI)).at(this.EthContractAddress);

          // this._contractInfura.mntpToken((error, address) => {
          //   this.EthMntpContractAddress = address;
          // });

          this.getContractABI(this.EthMntpContractAddress).subscribe(abi => {
            this.EthMntpContractABI = abi['result'];
          });
          this.getContractABI(this.EthGoldContractAddress).subscribe(abi => {
            this.EthGoldContractABI = abi['result'];
            this._contractHotGold = this._web3Infura.eth.contract(JSON.parse(this.EthGoldContractABI)).at(this.EthGoldContractAddress);
          });
          this.getContractABI(this.EthPoolContractAddress).subscribe(abi => {
            this.EthPoolContractABI = abi['result'];
          });

         //  this._contractInfura.goldToken((error, address) => {
         //    this.EthGoldContractAddress = address;
         //
         //    this.getContractABI(this.EthGoldContractAddress).subscribe(abi => {
         //      this.EthGoldContractABI = abi['result'];
         //      this._contractHotGold = this._web3Infura.eth.contract(JSON.parse(this.EthGoldContractABI)).at(this.EthGoldContractAddress);
         //    });
         // });

        } else {
          this._web3Infura = null;
        }
      });
    }

    if (!this._web3Metamask && (window.hasOwnProperty('web3') || window.hasOwnProperty('ethereum')) && this.EthGoldContractABI && this.EthMntpContractABI && this.EthPoolContractABI) {
      let ethereum = window['ethereum'];

      if (ethereum) {
        this._web3Metamask = new Web3(ethereum);
        ethereum.enable().then();
      } else {
        this._web3Metamask = new Web3(window['web3'].currentProvider);
      }

      if (this._web3Metamask.eth) {
        this._contractMetamask = this._web3Metamask.eth.contract(JSON.parse(this.EthContractABI)).at(this.EthContractAddress);
        this._contractGold = this._web3Metamask.eth.contract(JSON.parse(this.EthGoldContractABI)).at(this.EthGoldContractAddress);
        this.contractMntp = this._web3Metamask.eth.contract(JSON.parse(this.EthMntpContractABI)).at(this.EthMntpContractAddress);
        this.poolContract = this._web3Metamask.eth.contract(JSON.parse(this.EthPoolContractABI)).at(this.EthPoolContractAddress);

        this.isPoolContractLoaded$.next(true);
      } else {
        this._web3Metamask = null;
      }
    }

    if (!this._contactsInitted && this._userId) {
      this._contactsInitted = true;
      this.checkBalance();
    }

    if (this._web3Metamask && this._web3Metamask.version.network !== this._metamaskNetwork) {
      this._metamaskNetwork = this._web3Metamask.version.network;
      this._obsNetworkSubject.next(this._metamaskNetwork);
    }

    var addr = this._web3Metamask && this._web3Metamask.eth && this._web3Metamask.eth.accounts.length
      ? this._web3Metamask.eth.accounts[0] : null;

    if (this._lastAddress != addr) {
      window['ethereum'] && window['ethereum'].enable().then();
      this._lastAddress = addr;
      this.emitAddress(addr);
    }
  }

  private checkBalance() {
    if (this._lastAddress != null) {
      this.updateGoldBalance(this._lastAddress);
      this.updateMntpBalance(this._lastAddress);
    }
    this.updateEthBalance(this._lastAddress);

    this.checkHotBalance();
    this.updateTotalGoldBalances();
    this.updateEthLimitBalance(this.EthContractAddress);
  }

  private checkHotBalance() {
    this._userId != null && this._contractInfura && this._contractInfura.getUserHotGoldBalance(this._userId, (err, res) => {
      this._obsHotGoldBalanceSubject.next(res.div(new BigNumber(10).pow(18)));
    });
  }

  private emitAddress(ethAddress: string) {
    this._web3Metamask && this._web3Metamask['eth'] && this._web3Metamask['eth'].coinbase
    && (this._web3Metamask['eth'].defaultAccount = this._web3Metamask['eth'].coinbase);

    this._obsEthAddressSubject.next(ethAddress);
    this._obsGoldBalanceSubject.next(null);
    this._obsMntpBalanceSubject.next(null);
    this.checkBalance();
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
      this.contractMntp.balanceOf(addr, (err, res) => {
        this._obsMntpBalanceSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
      });
    }
  }

  private updateEthBalance(addr: string) {
    if (addr == null || this._contractGold == null) {
      this._obsEthBalanceSubject.next(null);
    } else {
      this._contractGold._eth.getBalance(addr, (err, res) => {
        this._obsEthBalanceSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
      });
    }
  }

  private updateEthLimitBalance(addr: string) {
    this._contractMetamask && this._web3Metamask.eth.getBalance(addr, (err, res) => {
      this._obsEthLimitBalanceSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
    });
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

  private getGasPrice() {
    this._http.get(this._gasPriceLink).subscribe(data => {
      this._obsGasPriceSubject.next(data['fast']);
    });
  }

  public isValidAddress(addr: string): boolean {
    return (new Web3()).isAddress(addr);
  }

  public getEthAddress(): string | null {
    return this._obsEthAddressSubject.value;
  }

  public getObservableEthAddress(): Observable<string> {
    return this._obsEthAddress;
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

  public getObservableEthBalance(): Observable<BigNumber> {
    return this._obsEthBalance;
  }

  public getObservableEthLimitBalance(): Observable<BigNumber> {
    return this._obsEthLimitBalance;
  }

  public getObservableTotalGoldBalances(): Observable<Object> {
    return this._obsTotalGoldBalances;
  }

  public getObservableNetwork(): Observable<Number> {
    return this._obsNetwork;
  }

  public getObservableGasPrice(): Observable<Object> {
    this.getGasPrice();
    return this._obsGasPrice;
  }

  public sendBuyRequest(fromAddr: string, userID: string, requestId: number, amount: string, gasPrice: number) {
    if (this._contractMetamask == null) return;
    const reference = new BigNumber(requestId);

    this._contractMetamask.addBuyTokensRequest(userID, reference.toString(), { from: fromAddr, value: amount, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      this.getSuccessBuyRequestLink$.next(res);
    });
  }

  public sendSellRequest(fromAddr: string, userID: string, requestId: number, amount: string, gasPrice: number) {
    if (this._contractMetamask == null) return;
    const reference = new BigNumber(requestId);

    this._contractMetamask.addSellTokensRequest(userID, reference.toString(), amount, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      this.getSuccessSellRequestLink$.next(res);
    });
  }

  public transferGoldToWallet(fromAddr: string, toAddr: string, amount: string, gasPrice: number) {
    if (this._contractGold == null) return;
    this._contractGold.transfer(toAddr, amount, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      this.getSuccessSellRequestLink$.next(res);
    });
  }

  public goldTransferMigration(fromAddr: string, toAddr: string, amount: string, gasPrice: number) {
    if (this._contractGold == null) return;
    this._contractGold.transfer(toAddr, amount, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      this.getSuccessMigrationGoldLink$.next(res);
    });
  }

  public mntpTransferMigration(fromAddr: string, toAddr: string, amount: string, gasPrice: number) {
    if (this.contractMntp == null) return;
    this.contractMntp.transfer(toAddr, amount, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      this.getSuccessMigrationMntpLink$.next(res);
    });
  }
}
