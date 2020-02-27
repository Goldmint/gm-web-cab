import { Injectable } from "@angular/core";
import { Observable } from "rxjs/Observable";
import * as Web3 from "web3";
import { BehaviorSubject } from "rxjs/BehaviorSubject";
import { UserService } from "./user.service";
import { BigNumber } from 'bignumber.js'
import {environment} from "../../environments/environment";
import {HttpClient} from "@angular/common/http";
import {Subject} from "rxjs/Subject";
import {NavigationEnd, Router} from "@angular/router";
import {APIService} from "./api.service";
import {combineLatest} from "rxjs/observable/combineLatest";

@Injectable()
export class EthereumService {
  private _etherscanGetABIUrl = environment.etherscanGetABIUrl;
  private _gasPriceLink = environment.gasPriceLink;
  private _etherscanApiKey = environment.etherscanApiKey;
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

  public poolContract: any;
  public contractMntp: any;
  public swapContract: any;

  private Web3 = new Web3();
  private _allowedUrlOccurrencesForInject = [
    'sell', 'buy', 'master-node', 'ethereum-pool', 'swap-mntp'
  ];
  private checkBalanceInterval = null;

  private _obsEthAddressSubject = new BehaviorSubject<string>(null);
  private _obsEthAddress: Observable<string> = this._obsEthAddressSubject.asObservable();
  private _obsMntpBalanceSubject = new BehaviorSubject<BigNumber>(null);
  private _obsMntpBalance: Observable<BigNumber> = this._obsMntpBalanceSubject.asObservable();
  private _obsGasPriceSubject = new BehaviorSubject<Object>(null);
  private _obsGasPrice: Observable<Object> = this._obsGasPriceSubject.asObservable();
  private _obsNetworkSubject = new BehaviorSubject<Number>(null);
  private _obsNetwork: Observable<Number> = this._obsNetworkSubject.asObservable();


  public isPoolContractLoaded$ = new BehaviorSubject(null);
  public getSuccessSwapMNTPLink$ = new Subject();

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _http: HttpClient,
    private router: Router
  ) {
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd && !this.checkBalanceInterval) {
        this._allowedUrlOccurrencesForInject.forEach(url => {
          if (event.urlAfterRedirects.indexOf(url) >= 0) {
            this.checkWeb3();
            this.checkBalanceInterval = setInterval(() => this.checkBalance(), 7500);
          }
        });
      }
    });
  }

  getContractABI(address: string) {
    return this._http.get(`${this._etherscanGetABIUrl}/api?module=contract&action=getabi&address=${address}&forma=raw&apikey=${this._etherscanApiKey}`);
  }

  private checkWeb3() {
    !this._web3Metamask && combineLatest(
      this.getContractABI(this.EthMntpContractAddress),
      this.getContractABI(this.EthPoolContractAddress),
      this.getContractABI(this.SwapContractAddress)
    ).subscribe(abi => {
      this.EthMntpContractABI = abi[0]['result'];
      this.EthPoolContractABI = abi[1]['result'];
      this.SwapContractABI = abi[2]['result'];

      const ethereum = window['ethereum'];

      if (ethereum && ethereum.isMetaMask && this.EthMntpContractABI && this.EthPoolContractABI && this.SwapContractABI) {
        this._web3Metamask = new Web3(ethereum);

        if (this._web3Metamask.eth) {
          this.contractMntp = this._web3Metamask.eth.contract(JSON.parse(this.EthMntpContractABI)).at(this.EthMntpContractAddress);
          this.poolContract = this._web3Metamask.eth.contract(JSON.parse(this.EthPoolContractABI)).at(this.EthPoolContractAddress);
          this.swapContract = this._web3Metamask.eth.contract(JSON.parse(this.SwapContractABI)).at(this.SwapContractAddress);

          this.isPoolContractLoaded$.next(true);
        } else {
          this._web3Metamask = null;
        }

        ethereum.on('accountsChanged', (accounts) => {
          if (accounts.length) {
            this.ethRequestAccounts(ethereum);
          } else {
            this._lastAddress = null;
            this.emitAddress(null);
          }
        });

        ethereum.on('networkChanged', network => {
          this._obsNetworkSubject.next(+network);
        });

        ethereum.send('eth_accounts').then(accounts => {
          if (accounts && accounts.result && accounts.result.length) {
            this.ethRequestAccounts(ethereum);
          }
        });

        ethereum.send('net_version').then(res => {
          this._obsNetworkSubject.next(+res.result);
        });
      }
    });
  }

  private ethRequestAccounts(ethereum) {
    ethereum.send('eth_requestAccounts').then(res => {
      const address = (res && res.result && res.result.length) ? res.result[0] : null;

      this._lastAddress = address;
      this.emitAddress(address);
    });
  }

  private emitAddress(address: string) {
    this._web3Metamask && this._web3Metamask['eth'] && this._web3Metamask['eth'].coinbase
    && (this._web3Metamask['eth'].defaultAccount = this._web3Metamask['eth'].coinbase);

    this._obsEthAddressSubject.next(address);
    this.checkBalance();
  }

  private checkBalance() {
    this.updateMntpBalance(this._lastAddress);
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
    window['ethereum'] && window['ethereum'].send('eth_requestAccounts').then();
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
