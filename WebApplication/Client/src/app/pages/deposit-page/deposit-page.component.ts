import {
  Component,
  OnInit,
  ViewEncapsulation,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  EventEmitter
} from '@angular/core';

import { CurrencyPipe } from '@angular/common';

// import { TabsetComponent } from 'ngx-bootstrap';

import { TFAInfo, CardsList, CardsListItem, Country, FiatLimits, SwiftInvoice } from '../../interfaces';
import { APIService, MessageBoxService } from '../../services';

import * as countries from '../../../assets/data/countries.json';
import { User } from "../../interfaces/user";
import { UserService } from "../../services/user.service";
import { Observable } from "rxjs/Observable";

enum Pages { Default, CardsList, BankTransfer }
enum BankTransferSteps { Default, Form, PaymentDetails }

@Component({
  selector: 'app-deposit-page',
  templateUrl: './deposit-page.component.html',
  styleUrls: ['./deposit-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DepositPageComponent implements OnInit {
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
  public agreeCheck: boolean = false;
  public buttonBlur = new EventEmitter<boolean>();
  public errors = [];
  public riskChecked: boolean = false; // use in Bank Transfer steps

  public countries: Country[];
  public limits: FiatLimits;
  public user: User;
  public invoiceData:SwiftInvoice;
  public swiftDepositChecked:boolean = false;
  public cardDepositChecked:boolean = false;

  constructor(
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef,
    private _user: UserService,
    private _messageBox: MessageBoxService
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
      this._cdRef.markForCheck();
    });
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

  submit() {
    this.processing = true;
    this.buttonBlur.emit();

    this._apiService.cardDeposit(this.depositModel.cardId, this.depositModel.amount)
      .finally(() => {
        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
      (/* res */) => this._messageBox.alert('Your request is being processed'), // TODO: Maybe will be better to use pipes from RxJS
      err => {
        if (err.error && err.error.errorCode) {
          switch (err.error.errorCode) {
            case 100: // InvalidParameter
              for (let i = err.error.data.length - 1; i >= 0; i--) {
                this.errors[err.error.data[i].field] = err.error.data[i].desc;
              }
              break;

            case 1005: // AccountDepositLimit
              this._messageBox.alert('Deposit limit reached');
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

