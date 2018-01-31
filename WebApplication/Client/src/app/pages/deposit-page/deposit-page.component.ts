import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter } from '@angular/core';
import { TabsetComponent } from 'ngx-bootstrap';

import { TFAInfo, CardsList, CardsListItem } from '../../interfaces';
import { APIService, MessageBoxService } from '../../services';

enum Pages {Default, CardsList}

@Component({
  selector: 'app-deposit-page',
  templateUrl: './deposit-page.component.html',
  styleUrls: ['./deposit-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DepositPageComponent implements OnInit {

  private _pages = Pages;

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
        res => {
          this.tfaInfo = res.data;
        },
        err => {});

     this.page = Pages.Default;
  }

  ngOnInit() {
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
            err => {});
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

    this._apiService.cardDeposit(this.depositModel.cardId, this.depositModel.amount)
      .finally(() => {
        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        res => {
          this._messageBox.alert('Successfully completed');
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

}
