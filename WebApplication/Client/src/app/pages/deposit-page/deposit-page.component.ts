import {
  Component,
  OnInit,
  ViewEncapsulation,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  EventEmitter, ViewChild, OnDestroy
} from '@angular/core';

import { TFAInfo, CardsList, CardsListItem, Country, FiatLimits, SwiftInvoice } from '../../interfaces';
import {APIService, EthereumService, MessageBoxService} from '../../services';

import * as countries from '../../../assets/data/countries.json';
import { User } from "../../interfaces/user";
import { UserService } from "../../services/user.service";
import { Observable } from "rxjs/Observable";
import {TranslateService} from "@ngx-translate/core";
import {BigNumber} from "bignumber.js";
import {interval} from "rxjs/observable/interval";
import {Subscription} from "rxjs/Subscription";

enum Pages { Default, CardsList, BankTransfer, CardsListSuccess, CryptoCapital }
enum BankTransferSteps { Default, Form, PaymentDetails }

@Component({
  selector: 'app-deposit-page',
  templateUrl: './deposit-page.component.html',
  styleUrls: ['./deposit-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DepositPageComponent implements OnInit {
  @ViewChild('depositForm') depositForm;

  public pages = Pages;
  private _bankTransferSteps = BankTransferSteps;

  public page: Pages;
  public bankTransferStep: BankTransferSteps;

  public loading: boolean = true;
  public processing: boolean = false;
  public tfaInfo: TFAInfo;
  public cards: CardsList;

  public limitsIncrease: boolean = true; //@todo: dev

  public depositModel: any = {};
  public buttonBlur = new EventEmitter<boolean>();
  public errors = [];
  public riskChecked: boolean = false; // use in Bank Transfer steps

  public countries: Country[];
  public limits: FiatLimits;
  public user: User;
  public invoiceData:SwiftInvoice;
  public swiftDepositChecked:boolean = false;
  public cardDepositChecked:boolean = false;

  public minAmount: number;
  public maxAmount: number;

  public ethAddress: string = '';
  public coinList = ['btc', 'eth']
  public currentCoin = this.coinList[1];
  public cryptoCurrencyAmount;
  public ethRate: number;
  public estimatedAmount;

  constructor(
    private _apiService: APIService,
    private _ethService: EthereumService,
    private _cdRef: ChangeDetectorRef,
    private _user: UserService,
    private _messageBox: MessageBoxService,
    private _translate: TranslateService
  ) {
    this.page = Pages.Default;
    this.countries = <Country[]><any>countries;
  }

  ngOnInit() {

    Observable.zip(
      this._apiService.getTFAInfo(),
      this._apiService.getLimits(),
      this._user.currentUser
    ).subscribe(res => {
      this.tfaInfo = res[0].data;
      this.limits = res[1].data;
      this.user = res[2];

      this.loading = false;
      this.minAmount = this.limits.paymentMethod.card.deposit.min;
      this.maxAmount = this.limits.current.deposit.minimal;
      this._cdRef.markForCheck();
    });

    this._apiService.getBannedCountries().subscribe((list) => {
      this.countries = this.countries.filter(item => list.data.indexOf(item.countryShortCode) < 0);
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableEthAddress().subscribe(ethAddr => {
      this.ethAddress = ethAddr;
    });

    this._apiService.getEthereumRate().subscribe(data => this.ethRate = data.data.usd);

  }

  goto(page: Pages) {
    switch (page) {
      case Pages.CardsList:
        this.loading = true;
        this.page = page;

        this._apiService.getFiatCards()
          .finally(() => {
            this.loading = false;
            this._cdRef.detectChanges();
          })
          .subscribe(
          res => {
            this.cards = res.data;
            this.cards.list = this.cards.list.filter((card: CardsListItem) => card.status === 'verified');
          }/* ,
            err => {} */);
        break;
      case Pages.BankTransfer:
        this.page = page;
        this.nextStep(BankTransferSteps.Default);
        break;

      case Pages.CryptoCapital:
        this.page = page;
        break;

      default:
        // code...
        break;
    }

    this._cdRef.detectChanges();
  }

  /**
   * The control of Bank Transfer steps
   */
  nextStep(step: BankTransferSteps) {
    switch (step) {
      case BankTransferSteps.Default:
        this.bankTransferStep = step;
        break;
      case BankTransferSteps.Form:
        if (this.riskChecked)
          this.bankTransferStep = step;
        break;
      case BankTransferSteps.PaymentDetails:
        this.bankTransferStep = step;
        break;
      default:
        // code...
        break;
    }
  }

  chooseCurrentCoin(coin) {
    if (this.currentCoin !== coin) {
      this.currentCoin = coin;
    }
  }

  onCryptoCurrencyChanged(value) {
    if (value != null && value > 0) {
      this.cryptoCurrencyAmount = new BigNumber(value);
      this.cryptoCurrencyAmount = this.cryptoCurrencyAmount.decimalPlaces(6, BigNumber.ROUND_DOWN);
      this.estimatedAmount = value * this.ethRate;
    } else {
      this.estimatedAmount = 0;
    }
  }

  onCryptoCurrencySubmit() {
    this.loading = true;
    this._apiService.ethDepositRequest(this.ethAddress, this.cryptoCurrencyAmount)
      .finally(() => {
        this.loading = false;
        this._cdRef.markForCheck();
      })
      .subscribe(res => {
        const amount = (this.cryptoCurrencyAmount * res.data.ethRate).toFixed(2)
        this._translate.get('MessageBox.EthDeposit',
          {coinAmount: this.cryptoCurrencyAmount, usdAmount: amount, ethRate: res.data.ethRate}
        ).subscribe(phrase => {
          this._messageBox.confirm(phrase).subscribe(ok => {
            if (ok) {
              this._apiService.confirmEthDepositRequest(true, res.data.requestId).subscribe(() => {
                this._ethService.ethDepositRequest(this.ethAddress, res.data.requestId);
              });
            }
          });
        });
      });
  }

  submit() {
    this.processing = true;
    this.buttonBlur.emit();

    this._apiService.cardDeposit(this.depositModel.cardId, this.depositModel.amount)
      .finally(() => {
        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
      () => { // TODO: Maybe will be better to use pipes from RxJS
        this.depositForm.reset();
        this.page = this.pages.CardsListSuccess;
        this._cdRef.detectChanges();
      }, err => {
        if (err.error && err.error.errorCode) {
          switch (err.error.errorCode) {
            case 100: // InvalidParameter
              for (let i = err.error.data.length - 1; i >= 0; i--) {
                this.errors[err.error.data[i].field] = err.error.data[i].desc;
              }
              break;

            case 1005: // AccountDepositLimit
              this._translate.get('MessageBox.DepositLimit').subscribe(phrase => {
                this._messageBox.alert(phrase);
              });
              break;

            default:
              this._messageBox.alert(err.error.errorDesc);
              break;
          }
        }
      });
  }

  submitTransferForm(amount) {
      this._apiService.getSwiftDepositInvoice(amount)
          .finally(()=> {

        this._cdRef.detectChanges();
          })
          .subscribe(res => {
            this.invoiceData = res.data;
          });
      this.nextStep(this._bankTransferSteps.PaymentDetails)
  }

  printInvoice() {
      let html = document.getElementById('invoice-wrapper').innerHTML;
      let win = window.open('');
        win.document.write(html);
        win.print();
        win.close();
  }

  downloadInvoice(e) {
      let html = document.getElementById('invoice-wrapper').innerHTML;
    let target = e.target;
    target.href='data:text/html;charset=UTF-8,'+ encodeURIComponent(html);
  }

  checkSwiftDeposit(val:number) {
    this.swiftDepositChecked = val <= this.limits.current.deposit.minimal
        && val >= this.limits.paymentMethod.swift.deposit.min
        && val <= this.limits.paymentMethod.swift.deposit.max;
  }

  checkCardDeposit(val:number) {
    this.cardDepositChecked = val <= this.limits.current.deposit.minimal
    && val >= this.limits.paymentMethod.card.deposit.min
    && val <= this.limits.paymentMethod.card.deposit.max;
  }

}

