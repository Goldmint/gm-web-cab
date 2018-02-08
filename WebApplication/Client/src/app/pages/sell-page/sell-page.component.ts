import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, HostBinding
} from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import * as Web3 from 'web3';
import { Subscription } from 'rxjs/Subscription';
import { FormGroup } from '@angular/forms';
import { Observable } from "rxjs/Observable";
import { BigNumber } from 'bignumber.js'

@Component({
  selector: 'app-sell-page',
  templateUrl: './sell-page.component.html',
  styleUrls: ['./sell-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SellPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  locale: string;
  progress: boolean = false;
  confirmation: boolean = false;

  toSellUnset: boolean = true;
  toSell: BigNumber = new BigNumber (0);
  estimate_amount:number = 0;
  commission: number = 0;

  goldBalance: BigNumber = null;
  mntpBalance: BigNumber = null;
  goldUsdRate: number = null;
  estimatesAmount: string = null;
 
  commissionArray: number[] = [3, 2.5, 1.5, 0.75];
  mntpArray: number[] = [10, 1000, 10000];
  buyMNT_DisableArray = [false, false, false, false];
  buyMNTArray = [10, 1000, 10000];
  discountUSDArray: number[] = [0, 0, 0, 0];

  //form: FormGroup;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {

    this.toSellUnset = true;

    this._userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
      this._cdRef.detectChanges();
    });

    Observable.combineLatest(
      this._ethService.getObservableGoldBalance(),
      this._goldrateService.getObservableRate(),
      this._ethService.getObservableMntpBalance()
    )
      .subscribe((data) => {
        if (data[0] !== null) this.goldBalance = data[0];
        if (data[1] !== null) this.goldUsdRate = data[1];
        if (data[2] !== null) this.mntpBalance = data[2];

        if (this.goldBalance !== null && this.goldBalance.gt(0) && this.toSellUnset) {
          this.toSellUnset = false;
          this.toSell = this.goldBalance;
        }

        if (this.goldBalance !== null && this.goldUsdRate !== null && this.mntpBalance !== null) {
          this.getDataCommission(this.goldUsdRate, this.goldBalance, this.mntpBalance);
        }

        if (!this.progress && !this.confirmation) {
          this._cdRef.detectChanges();
        }
      });

  }

  onToSellChanged(value:string) {
    this.toSellUnset = false;
    this.toSell = new BigNumber(0);
    if (value != '') this.toSell = new BigNumber(value);
    this.getDataCommission(this.goldUsdRate, this.goldBalance, this.mntpBalance);
    this.estimate_amount = 0;
    this._cdRef.detectChanges();
  }

  onSetSellPercent(percent:number) {
    var goldBalancePercent = new BigNumber(0);
    if (this.goldBalance != null) goldBalancePercent = new BigNumber(this.goldBalance.times(percent));
    this.toSell = goldBalancePercent;
    this.getDataCommission(this.goldUsdRate, this.goldBalance, this.mntpBalance);
  }

  estimatesAmountDecor(price) {
    return price.toFixed(2).replace(/(\d)(?=(\d\d\d)+([^\d]|$))/g, '$1 ');
  }

  getDataCommission(rate: number, gold: BigNumber, mntp: BigNumber) {
    this.calculationDiscount(mntp);
    this.calculationData(mntp);

    const amountCents = Math.floor(this.toSell.times(this.goldUsdRate).times(100).toNumber());

    for (let i = 0; i < this.discountUSDArray.length; i++) {
      const feeCents = Math.floor(this.commissionArray[i] * amountCents / 100);

      this.discountUSDArray[i] = (amountCents - feeCents) / 100;
      if (this.commissionArray[i] === this.commission) {
        this.estimatesAmount = this.estimatesAmountDecor(this.discountUSDArray[i]);
      }
    }
  }

  calculationDiscount(mntp) {
    if (mntp < this.mntpArray[0]) {
      this.commission = this.commissionArray[0];
    } else if (mntp >= this.mntpArray[0] && mntp < this.mntpArray[1]) {
      this.commission = this.commissionArray[1];
    } else if (mntp >= this.mntpArray[1] && mntp < this.mntpArray[2]) {
      this.commission = this.commissionArray[2];
    } else {
      this.commission = this.commissionArray[3];
    }
  }

  calculationData(mntp) {
    this.buyMNTArray = [10 - mntp, 1000 - mntp, 10000 - mntp];

    this.buyMNT_DisableArray[0] = false;
    this.buyMNT_DisableArray[1] = false;
    this.buyMNT_DisableArray[2] = false;

    if (mntp < this.mntpArray[0]) {
    } else if (mntp >= this.mntpArray[0] && mntp < this.mntpArray[1]) {
      this.buyMNT_DisableArray[0] = true;
    } else if (mntp >= this.mntpArray[1] && mntp < this.mntpArray[2]) {
      this.buyMNT_DisableArray[0] = true;
      this.buyMNT_DisableArray[1] = true;
    } else {
      this.buyMNT_DisableArray[0] = true;
      this.buyMNT_DisableArray[1] = true;
      this.buyMNT_DisableArray[2] = true;
    }
  }

  onSell() {
    var ethAddress = this._ethService.getEthAddress();

    if (ethAddress == null) {
      this._messageBox.alert('Enable metamask first');
      return;
    }

    this.progress = true;
    this._cdRef.detectChanges();

    this._apiService.goldSellReqest(ethAddress, this.toSell)
      .finally(() => {
        this.progress = false;
        this._cdRef.detectChanges();
      })
      .subscribe(res => {
        var confText =
          "GOLD to sell: " + this.toSell + "<br/>" +
          "You will get: $ " + res.data.fiatAmount + " ($ " + res.data.feeAmount + " fee)<br/>" +
          "GOLD/USD: $ " + res.data.goldRate
          ;

        this.confirmation = true;
        this._cdRef.detectChanges();

        this._messageBox.confirm(confText).subscribe(ok => {
          this.confirmation = false;
          if (ok) {
            this._ethService.sendSellRequest(ethAddress, res.data.payload);
          }
          this._cdRef.detectChanges();
        });
      },
      err => {
        if (err.error && err.error.errorCode) {
          this._messageBox.alert(err.error.errorDesc);
        }
      });
  }
}
