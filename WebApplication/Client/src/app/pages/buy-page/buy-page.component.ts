import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, OnDestroy,
  ViewChild
} from '@angular/core';
import { UserService, APIService, MessageBoxService, EthereumService, GoldrateService } from '../../services';
import { GoldBuyResponse } from '../../interfaces'
import { Subscription } from 'rxjs/Subscription';
import { Observable } from "rxjs/Observable";
import { BigNumber } from 'bignumber.js'
import {Router} from "@angular/router";
import {TranslateService} from "@ngx-translate/core";

@Component({
  selector: 'app-buy-page',
  templateUrl: './buy-page.component.html',
  styleUrls: ['./buy-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'page' }
})
export class BuyPageComponent implements OnInit, OnDestroy {

  @ViewChild('inputToSpend') inputToSpend;

  confirmation: boolean = false;
  progress: boolean = false;
  toSpendUnset: boolean = true;
  toSpend: BigNumber = new BigNumber(0);

  usdBalance: number = 0;
  goldUsdRate: number = 0;
  estimatedAmount: string = "";
  public buyAmountChecked: boolean = true;
  public ethAddress: string = '';
  public selectedWallet = 0;

  private sub1: Subscription;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef,
    private router: Router,
    private _translate: TranslateService
  ) { }

  ngOnInit() {
    Observable.combineLatest(this._ethService.getObservableUsdBalance(), this._goldrateService.getObservableRate())
      .subscribe((data) => {
        if (data[0] !== null) this.usdBalance = data[0];
        if (data[1] !== null) this.goldUsdRate = data[1];

        // got first time
        if (this.toSpendUnset && this.usdBalance > 0) {
          this.toSpendUnset = false;
          this.toSpend = new BigNumber(this.usdBalance);
          this.inputToSpend.nativeElement.value = this.toSpend.toNumber();
          this.buyAmountCheck(this.toSpend);
        }

        if (!this.progress && !this.confirmation) {
          this.estimate(this.toSpend);
        }

        this._cdRef.markForCheck();

      });

    this._ethService.getObservableEthAddress().subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress) {
        this.selectedWallet = 0;
      }
    });

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;

    this.sub1 = this._userService.onWalletSwitch$.subscribe((wallet) => {
      this.selectedWallet = wallet['id'] === 'hot' ? 0 : 1;
      this._cdRef.markForCheck();
    });
  }

  onToSpendChanged(value: string) {
    this.toSpendUnset = false;

    this.toSpend = new BigNumber(0);
    var testVal = this.usdBalance && value && value.length > 0 ? parseFloat(value).toFixed(2) : 0;

    if (testVal > 0) {
      this.toSpend = (new BigNumber(testVal)).decimalPlaces(2, BigNumber.ROUND_DOWN);
      this.inputToSpend.nativeElement.value = this.toSpend.toNumber();
    }

    this.estimate(this.toSpend);
    this.buyAmountCheck(this.toSpend);
    this._cdRef.markForCheck();
  }

  setUsdBalance(percent) {
    const amount = this.usdBalance * percent;
    this.onToSpendChanged(amount.toString());
  }

  estimate(amount: BigNumber) {
    var toConvert = this.usdBalance ? BigNumber.min(amount, new BigNumber(this.usdBalance)) : new BigNumber(0);
    this.estimatedAmount = toConvert.dividedBy(this.goldUsdRate).toPrecision(18 + 1);
    this.estimatedAmount = this.estimatedAmount.substr(0, this.estimatedAmount.length - 1);
  }

  onBuy() {
    this.progress = true;
    this._cdRef.markForCheck();

    if (this.selectedWallet == 0) {
      this._apiService.goldBuyHwRequest(this.toSpend.toNumber())
        .finally(() => {
          this.progress = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
          const amount = new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18));
          this.confirmation = true;
          this._cdRef.markForCheck();

          this._translate.get('MessageBox.GoldBuy',
            {goldAmount: amount, goldRate: res.data.goldRate}
          ).subscribe(phrase => {
              this.confirmation = false;
              this._messageBox.confirm(phrase).subscribe(ok => {
                if (ok) {
                  this._apiService.confirmHwRequest(true, res.data.requestId).subscribe(() => {
                    this._translate.get('MessageBox.RequestProgress').subscribe(phrase => {
                      this._messageBox.alert(phrase).subscribe(() => {
                        this.router.navigate(['/finance/history']);
                      });
                    });
                  });
                }
                this._cdRef.markForCheck();
              });
            });
          });
    } else {
      this._apiService.goldBuyRequest(this.ethAddress, this.toSpend.toNumber())
        .finally(() => {
          this.progress = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
          const amount = new BigNumber(res.data.goldAmount).dividedBy(new BigNumber(10).pow(18));
          this.confirmation = true;
          this._cdRef.markForCheck();

          this._translate.get('MessageBox.GoldBuy',
            {goldAmount: amount, goldRate: res.data.goldRate}
          ).subscribe(phrase => {
            this.confirmation = false;
            this._messageBox.confirm(phrase).subscribe(ok => {
              if (ok) {
                this._ethService.sendBuyRequest(this.ethAddress, res.data.payload);
              }
              this._cdRef.markForCheck();
            });
          });
        });
    }
  }

  buyAmountCheck(val: BigNumber) {
    this.buyAmountChecked = val.gte(1) && this.usdBalance && val.lte(this.usdBalance);
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }
}
