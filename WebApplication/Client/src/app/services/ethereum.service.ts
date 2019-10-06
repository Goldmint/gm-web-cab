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
import {APIService} from "./api.service";

@Injectable()
export class EthereumService {
  private _etherscanGetABIUrl = environment.etherscanGetABIUrl;
  private _gasPriceLink = environment.gasPriceLink;
  // mntp token
  private EthMntpContractAddress: string = environment.EthMntpContractAddress;
  private EthMntpContractABI: string;
  // pool contract
  public EthPoolContractAddress: string = environment.EthPoolContractAddress;
  private EthPoolContractABI: string;
  // swap contract
  public SwapContractAddress: string = environment.SwapContractAddress;
  private SwapContractABI: string;

  private _web3Metamask: Web3 = null;
  private _lastAddress: string | null;
  private _userId: string | null;
  private _metamaskNetwork: number = null

  public poolContract: any;
  public oldPoolContract: any;
  public contractMntp: any;
  public swapContract: any;

  private Web3 = new Web3();
  private _contactsInitted: boolean = false;
  private _allowedUrlOccurrencesForInject = [
    'sell', 'buy', 'master-node', 'ethereum-pool', 'swap-mntp'
  ];
  private checkWeb3Interval = null;

  private _obsEthAddressSubject = new BehaviorSubject<string>(null);
  private _obsEthAddress: Observable<string> = this._obsEthAddressSubject.asObservable();
  private _obsMntpBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsMntpBalance: Observable<BigNumber> = this._obsMntpBalanceSubject.asObservable();
  private _obsGasPriceSubject = new BehaviorSubject<Object>(null);
  private _obsGasPrice: Observable<Object> = this._obsGasPriceSubject.asObservable();
  private _obsNetworkSubject = new BehaviorSubject<Number>(null);
  private _obsNetwork: Observable<Number> = this._obsNetworkSubject.asObservable();
  private _obsSumusAccountSubject = new BehaviorSubject<any>(null);
  private _obsSumusAccount: Observable<any> = this._obsSumusAccountSubject.asObservable();

  private updateGoldBalanceInterval;

  public isPoolContractLoaded$ = new BehaviorSubject(null);

  public getSuccessSellRequestLink$ = new Subject();
  public getSuccessSwapMNTPLink$ = new Subject();

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _http: HttpClient,
    private router: Router
  ) {
    this._userService.currentUser.subscribe(currentUser => {
      if (currentUser != null) {
        this._userId = currentUser.id ? currentUser.id : null;

        clearInterval(this.updateGoldBalanceInterval);
        if (currentUser.hasOwnProperty('id')) {
          this.getSumusAccountInfo();
          this.updateGoldBalanceInterval = setInterval(() => this.getSumusAccountInfo(), 7500);
        }
      }
    });

    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd && !this.checkWeb3Interval) {
        this._allowedUrlOccurrencesForInject.forEach(url => {
          if (event.urlAfterRedirects.indexOf(url) >= 0) {
            this.checkWeb3Interval = interval(500).subscribe(this.checkWeb3.bind(this));
            interval(7500).subscribe(this.checkBalance.bind(this));
          }
        });
      }
    });
  }

  getContractABI(address) {
    return this._http.get(`${this._etherscanGetABIUrl}/api?module=contract&action=getabi&address=${address}&forma=raw`)
  }

  private checkWeb3() {
    this.getContractABI(this.EthMntpContractAddress).subscribe(abi => {
      this.EthMntpContractABI = abi['result'];
    });

    this.getContractABI(this.EthPoolContractAddress).subscribe(abi => {
      this.EthPoolContractABI = abi['result'];
    });

    /*this.getContractABI(this.SwapContractAddress).subscribe(abi => {
        this.SwapContractABI = abi['result'];
    });*/

    if (!this._web3Metamask && (window.hasOwnProperty('web3') || window.hasOwnProperty('ethereum')) && this.EthMntpContractABI && this.EthPoolContractABI /*&& this.SwapContractABI*/) {
      let ethereum = window['ethereum'];

      if (ethereum) {
        this._web3Metamask = new Web3(ethereum);
      } else {
        this._web3Metamask = new Web3(window['web3'].currentProvider);
      }

      if (this._web3Metamask.eth) {
        this.contractMntp = this._web3Metamask.eth.contract(JSON.parse(this.EthMntpContractABI)).at(this.EthMntpContractAddress);
        this.poolContract = this._web3Metamask.eth.contract(JSON.parse(this.EthPoolContractABI)).at(this.EthPoolContractAddress);
        // this.swapContract = this._web3Metamask.eth.contract(JSON.parse(this.SwapContractABI)).at(this.SwapContractAddress);

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
      this.updateMntpBalance(this._lastAddress);
    }
  }

  private emitAddress(ethAddress: string) {
    this._web3Metamask && this._web3Metamask['eth'] && this._web3Metamask['eth'].coinbase
    && (this._web3Metamask['eth'].defaultAccount = this._web3Metamask['eth'].coinbase);

    this._obsEthAddressSubject.next(ethAddress);
    this._obsMntpBalanceSubject.next(null);
    this.checkBalance();
  }

  private updateMntpBalance(addr: string) {
    if (addr == null || this.contractMntp == null) {
      this._obsMntpBalanceSubject.next(null);
    } else {
      this.contractMntp.balanceOf(addr, (err, res) => {
        this._obsMntpBalanceSubject.next(new BigNumber(res.toString()).div(new BigNumber(10).pow(18)));
      });
    }
  }

  private getGasPrice() {
    this._http.get(this._gasPriceLink).subscribe(data => {
      this._obsGasPriceSubject.next(data['fast']);
    });
  }

  getSumusAccountInfo() {
    this._apiService.getUserAccount().subscribe((data: any) => {
      this._obsSumusAccountSubject.next(data.data);
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

  public getObservableMntpBalance(): Observable<BigNumber> {
    return this._obsMntpBalance;
  }

  public getObservableNetwork(): Observable<Number> {
    return this._obsNetwork;
  }

  public getObservableGasPrice(): Observable<Object> {
    this.getGasPrice();
    return this._obsGasPrice;
  }

  public getObservableSumusAccount(): Observable<any> {
    return this._obsSumusAccount;
  }

  public getPoolTokenAllowance(fromAddr: string) {
    return new Observable(observer => {
      this.contractMntp && this.contractMntp.allowance(fromAddr, this.SwapContractAddress, (err, res) => {
        if (res) {
          const allowance = +new BigNumber(res.toString()).div(new BigNumber(10).pow(18));
          observer.next(allowance);
          observer.complete();
        }
      });
    });
  }

  public connectToMetaMask() {
    const ethereum = window['ethereum'];
    ethereum && ethereum.enable().then();
  }

  public swapMNTP(sumusAddress, fromAddr: string, amount: number, gasPrice: number) {
    if (!this.swapContract || !this.contractMntp) return

    this.contractMntp.allowance(fromAddr, this.SwapContractAddress, (err, res) => {
      if (res) {
        const allowance = +new BigNumber(res.toString()).div(new BigNumber(10).pow(18));
        const wei = this.Web3.toWei(amount);

        if (allowance !== 0 && allowance !== amount) {
          this.contractMntp.approve(this.SwapContractAddress, 0, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
            res && setTimeout(() => {
              this._swapMntp(sumusAddress, fromAddr, wei, gasPrice);
            }, 1000);
          });
        } else {
          this._swapMntp(sumusAddress, fromAddr, wei, gasPrice);
        }
      }
    });
  }

  private _swapMntp(sumusAddress, fromAddr: string, wei: string, gasPrice: number) {
    this.contractMntp.approve(this.SwapContractAddress, wei, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
      res && setTimeout(() => {
        this.swapContract.swapMntp(wei, sumusAddress, { from: fromAddr, value: 0, gas: 214011, gasPrice: gasPrice }, (err, res) => {
          this.getSuccessSwapMNTPLink$.next(res);
        });
      }, 1000);
    });
  }

}
