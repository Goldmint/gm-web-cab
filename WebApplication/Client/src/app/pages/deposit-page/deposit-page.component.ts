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

import { TFAInfo, CardsList, CardsListItem, Country } from '../../interfaces';
import { APIService, MessageBoxService } from '../../services';
import {Limit} from "../../interfaces/limit";

import * as countries from '../../../assets/data/countries.json';

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
  private _pages = Pages;
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
  public limits = <Limit>{};

  constructor(
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService) {

    this.tfaInfo = {enabled: false} as TFAInfo;

    this._apiService.getTFAInfo()
      .finally(() => {
        this.loading = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        res => this.tfaInfo = res.data/* ,
        err => {} */
      );
      this._apiService.getLimits()
          .finally(() => {
          })
          .subscribe(res => {
                  this.limits = res.data.current.deposit;
                  this._cdRef.detectChanges();
              },
              err => {});

    this.page = Pages.Default;

    this.countries = <Country[]><any> countries;
  }

  ngOnInit() {}

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

  proceedTransfer() {
    console.log('Proceeded!');
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
}

