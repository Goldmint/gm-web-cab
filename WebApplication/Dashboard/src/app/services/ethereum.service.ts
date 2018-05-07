import { Injectable } from "@angular/core";
import { Observable } from "rxjs/Observable";
import { interval } from "rxjs/observable/interval";
import * as Web3 from "web3";
import { BehaviorSubject } from "rxjs/BehaviorSubject";
import { UserService } from "./user.service";
import {environment} from "../../environments/environment";
import {HttpClient} from "@angular/common/http";

@Injectable()
export class EthereumService {
  private _infuraUrl = environment.infuraUrl;
  private _etherscanGetABIUrl = environment.etherscanGetABIUrl;
  // main contract
  private EthContractAddress = environment.EthContractAddress;
  private EthContractABI: string;
  // gold token
  private EthGoldContractAddress: string
  private EthGoldContractABI: string;
  // mntp token
  private EthMntpContractAddress: string;
  private EthMntpContractABI: string;

  private _web3Infura: Web3 = null;

  private _contractInfura: any;
  public _contractGold: any;
  public _contractHotGold: any;
  private _totalGoldBalances = {issued: null, burnt: null};

  private _obsTotalGoldBalancesSubject = new BehaviorSubject<Object>(null);
  private _obsTotalGoldBalances: Observable<Object> = this._obsTotalGoldBalancesSubject.asObservable();

  constructor(
    private _userService: UserService,
    private _http: HttpClient
  ) {
    console.log('EthereumService constructor');

    interval(500).subscribe(this.checkWeb3.bind(this));

    interval(7500).subscribe(this.checkBalance.bind(this));
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

          this._contractInfura.mntpToken((error, address) => {
            this.EthMntpContractAddress = address;
          });

          this._contractInfura.goldToken((error, address) => {
            this.EthGoldContractAddress = address;

            this.getContractABI(this.EthGoldContractAddress).subscribe(abi => {
              this.EthGoldContractABI = this.EthMntpContractABI = abi['result'];

              this._contractHotGold = this._web3Infura.eth.contract(JSON.parse(this.EthGoldContractABI)).at(this.EthGoldContractAddress);
              this.checkBalance();
            });
         });

        } else {
          this._web3Infura = null;
        }
      });
    }
  }

  private checkBalance() {
    this.updateTotalGoldBalances();
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

  public getObservableTotalGoldBalances(): Observable<Object> {
    return this._obsTotalGoldBalances;
  }

}
