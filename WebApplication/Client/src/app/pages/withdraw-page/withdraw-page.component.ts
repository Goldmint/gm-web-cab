import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter,
  ViewChild
} from '@angular/core';
import { Observable } from "rxjs/Observable";

import { TFAInfo, CardsList, CardsListItem, FiatLimits } from '../../interfaces';
import { APIService, MessageBoxService } from '../../services';
import { UserService } from "../../services/user.service";
import { User } from "../../interfaces/user";

enum Pages { Default, CardsList, CardsListSuccess }

@Component({
  selector: 'app-withdraw-page',
  templateUrl: './withdraw-page.component.html',
  styleUrls: ['./withdraw-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WithdrawPageComponent implements OnInit {

  @ViewChild('depositForm') depositForm;

  public pages = Pages;

  public page: Pages;
  public loading: boolean = true;
  public processing: boolean = false;
  public tfaInfo: TFAInfo;
  public cards: CardsList;

  public limitsIncrease: boolean = true; //@todo: dev

  public depositModel: any = {};
  public agreeCheck: boolean = false;
  public buttonBlur = new EventEmitter<boolean>();
  public errors = [];
  public limits: FiatLimits;
  public user: User;
  public swiftWithdrawChecked:boolean = false;
  public cardWithdrawChecked:boolean = false;

  public minAmount: number;
  public maxAmount: number;

  constructor(
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef,
    private _user: UserService,
    private _messageBox: MessageBoxService
  ) {
    this.page = Pages.Default;
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
      this.maxAmount = this.limits.current.withdraw.minimal;
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
          },
          err => { });
        break;

      default:
        // code...
        break;
    }

    this._cdRef.detectChanges();
  }

  submit() {
    this.processing = true;
    this.buttonBlur.emit();

    this._apiService.cardWithdraw(this.depositModel.cardId, this.depositModel.amount)
      .finally(() => {
        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
      res => {
        this.depositForm.reset();
        this.page = this.pages.CardsListSuccess;
        this._cdRef.detectChanges();
      },
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

  checkSwiftWithdraw(val:number) {
    this.swiftWithdrawChecked = val <= this.limits.current.withdraw.minimal
      && val >= this.limits.paymentMethod.swift.withdraw.min
      && val <= this.limits.paymentMethod.swift.withdraw.max;
  }

  checkCardWithdraw(val:number) {
    this.cardWithdrawChecked = val <= this.limits.current.withdraw.minimal
      && val >= this.limits.paymentMethod.card.withdraw.min
      && val <= this.limits.paymentMethod.card.withdraw.max;
  }

}
