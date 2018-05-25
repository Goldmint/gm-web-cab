import {ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit} from '@angular/core';
import {APIService, EthereumService, MessageBoxService} from "../../services";
import {Router} from "@angular/router";
import {Subscription} from "rxjs/Subscription";
import {Subject} from "rxjs/Subject";
import {TranslateService} from "@ngx-translate/core";
import {environment} from "../../../environments/environment";
import * as Web3 from "web3";

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
  public isFirstTransaction: boolean = true;

  public sub1: Subscription;
  public subGetGas: Subscription;
  public etherscanUrl = environment.etherscanUrl;
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private Web3 = new Web3();

  @Input('amount') transferData;

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
  }

  buyMethod() {
    this.loading = true;
    let amount = this.transferData.reversed ? this.Web3.toWei(+this.transferData.amount) : (+this.transferData.amount * 100);

    this._apiService.goldBuyAsset(this.transferData.ethAddress, amount.toString(), this.transferData.reversed, this.transferData.currency)
      .subscribe(res => {
        this._apiService.buyGoldFiat(this.transferData.cardId, this.transferData.ethAddress, this.transferData.currency, amount.toString(), this.transferData.reversed)
          .finally(() => {
            this.loading = this.agreeCheck = false;
            this._cdRef.markForCheck();
          })
          .subscribe(() => {
            this._apiService.goldBuyConfirm(res.data.requestId).subscribe(() => {
              this._messageBox.alert('Transaction in Process').subscribe(() => {
                this.router.navigate(['/finance/history']);
              });
            });
          });
      });
  }

  sellMethod() {
    this.isFirstTransaction = this.loading = true;
    this.sub1 && this.sub1.unsubscribe();
    this.subGetGas && this.subGetGas.unsubscribe();

    let amount = this.transferData.reversed ? (+this.transferData.amount * 100) : this.Web3.toWei(+this.transferData.amount);
    let goldAmount =  this.Web3.toWei(+this.transferData.goldAmount);

    this._apiService.goldSellAsset(this.transferData.ethAddress, amount.toString(), this.transferData.reversed, this.transferData.currency)
      .subscribe((res) => {
        this._apiService.sellGoldFiat(this.transferData.cardId, this.transferData.ethAddress, this.transferData.currency, amount.toString(), this.transferData.reversed)
          .subscribe(() => {
            this._apiService.goldSellConfirm(res.data.requestId)
              .finally(() => {
                this.loading = this.agreeCheck = false;
                this._cdRef.markForCheck();
              })
              .subscribe(() => {

              this.subGetGas = this._ethService.getObservableGasPrice().subscribe((price) => {
                if (price !== null && this.isFirstTransaction) {
                  this._ethService.sendSellRequest(this.transferData.ethAddress, this.transferData.userId, res.data.requestId, goldAmount.toString(), +price);
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
