import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output
} from '@angular/core';
import {APIService, EthereumService, MessageBoxService} from "../../services";
import {Router} from "@angular/router";
import {Subscription} from "rxjs/Subscription";
import {Subject} from "rxjs/Subject";
import {TranslateService} from "@ngx-translate/core";
import {environment} from "../../../environments/environment";
import * as Web3 from "web3";
import {BigNumber} from "bignumber.js";

@Component({
  selector: 'app-payment-card-block',
  templateUrl: './payment-card-block.component.html',
  styleUrls: ['./payment-card-block.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PaymentCardBlockComponent implements OnInit, OnDestroy {

  public agreeCheck: boolean = false;
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isFirstTransaction: boolean = true;

  public sub1: Subscription;
  public subGetGas: Subscription;
  public etherscanUrl = environment.etherscanUrl;
  private buyRequestId: number;
  private sellRequestId: number;
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private Web3 = new Web3();
  private goldAmount: BigNumber;

  @Input('amount') transferData;
  @Output() hideForm: EventEmitter<any> = new EventEmitter();

  constructor(
    private _apiService: APIService,
    private _ethService: EthereumService,
    private _messageBox: MessageBoxService,
    private _cdRef: ChangeDetectorRef,
    private _translate: TranslateService,
    private router: Router
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 767);
    window.onresize = () => {
      this.isMobile = window.innerWidth <= 767 ? true : false;
      this._cdRef.markForCheck();
    };

    if (this.transferData.type === 'buy') {
      let amount = this.transferData.reversed ? this.Web3.toWei(+this.transferData.amount) : (+this.transferData.amount * 100);

      this._apiService.buyGoldFiat(this.transferData.cardId, this.transferData.ethAddress, this.transferData.currency, amount.toString(), this.transferData.reversed)
        .subscribe((res) => {
          if (this.transferData.reversed) {
            this.transferData.amountView = res.data.estimation.amount + ' USD';
            this.transferData.estimatedView = +this.transferData.amount + ' GOLD';
          } else {
            this.transferData.amountView = +this.transferData.amount + ' USD';
            this.transferData.estimatedView = this.substrValue(res.data.estimation.amount / Math.pow(10, 18)) + ' GOLD';
          }

          this.buyRequestId = res.data.requestId;
          this.isDataLoaded = true;
          this._cdRef.markForCheck();
        });
    }

    if (this.transferData.type === 'sell') {
      let amount = this.transferData.reversed ? (+this.transferData.amount * 100) : this.Web3.toWei(+this.transferData.amount);

      this._apiService.sellGoldFiat(this.transferData.cardId, this.transferData.ethAddress, this.transferData.currency, amount.toString(), this.transferData.reversed)
        .subscribe((res) => {
          if (this.transferData.reversed) {
            this.transferData.amountView = this.substrValue(res.data.estimation.amount / Math.pow(10, 18)) + ' GOLD';
            this.transferData.estimatedView = +this.transferData.amount + ' USD';
            this.goldAmount = res.data.estimation.amount;
          } else {
            this.transferData.amountView = +this.transferData.amount + ' GOLD';
            this.transferData.estimatedView = res.data.estimation.amount + ' USD';
            this.goldAmount = this.Web3.toWei(+this.transferData.goldAmount);
          }

          this.sellRequestId = res.data.requestId;
          this.isDataLoaded = true;
          this._cdRef.markForCheck();
        });
    }

  }

  substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d+)(?:(\.\d{0,6})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  hidePaymentCardForm() {
    this.hideForm.emit(true);
  }

  buyMethod() {
    this.loading = true;
    this._apiService.goldBuyConfirm(this.buyRequestId)
      .finally(() => {
        this.loading = this.agreeCheck = false;
        this._cdRef.markForCheck();
      }).subscribe(() => {
      this._messageBox.alert('Transaction in Process').subscribe(() => {
        this.router.navigate(['/finance/history']);
      });
    });
  }

  sellMethod() {
    this.isFirstTransaction = this.loading = true;
    this.sub1 && this.sub1.unsubscribe();
    this.subGetGas && this.subGetGas.unsubscribe();

    this._apiService.goldSellConfirm(this.sellRequestId)
      .finally(() => {
        this.loading = this.agreeCheck = false;
        this._cdRef.markForCheck();
      })
      .subscribe(() => {
        this.subGetGas = this._ethService.getObservableGasPrice().subscribe((price) => {
          if (price !== null && this.isFirstTransaction) {
            this._ethService.sendSellRequest(this.transferData.ethAddress, this.transferData.userId, this.sellRequestId, this.goldAmount.toString(), +price);
            this.isFirstTransaction = false;
          }
        });

        this.sub1 = this._ethService.getSuccessSellRequestLink$.subscribe(hash => {
          if (hash) {
            this._translate.get('PAGES.Sell.CtyptoCurrency.SuccessModal').subscribe(phrases => {
              this._messageBox.alert(`
                <div class="text-center">
                  <div class="font-weight-500 mb-2">${phrases.Heading}</div>
                  <div>${phrases.Steps}</div>
                  <div>${phrases.Hash}</div>
                  <div class="mb-2 sell-hash">${hash}</div>
                  <a href="${this.etherscanUrl}${hash}" target="_blank">${phrases.Link}</a>
                </div>
                `).subscribe(ok => {
                ok && this.router.navigate(['/finance/history']);
              });
            });
          }
        });
      });
  }

  onSubmit() {
    this.transferData.type === 'buy' ? this.buyMethod() : this.sellMethod();
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    this.subGetGas && this.subGetGas.unsubscribe();
    this.sub1 && this.sub1.unsubscribe();
  }

}
