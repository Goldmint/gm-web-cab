import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import 'rxjs/add/operator/finally';
import { zip } from 'rxjs/observable/zip';

import { User, CardsList, CardsListItem, APIResponse, CardStatusResponse } from '../../../interfaces';
import { UserService, APIService, MessageBoxService } from '../../../services';
import {TranslateService} from "@ngx-translate/core";

enum Page { List, OnNew, OnNeedConfirmation, OnNeedVerification, OnVerified, OnFailure }

@Component({
  selector: 'app-settings-cards-page',
  templateUrl: './settings-cards-page.component.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})

export class SettingsCardsPageComponent implements OnInit {

  private _pages = Page;

  public loading: boolean = true;
  public page = Page.List;

  public user: User;
  public processing: boolean;
  public buttonBlur = new EventEmitter<boolean>();
  public cards: CardsList;
  public currentCardId: number;
  public verificationCode: number;

  constructor(
    private _route: ActivatedRoute,
    private _router: Router,
    private _userService: UserService,
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService,
    private _translate: TranslateService
  ) {
    this._route.params
      .subscribe(params => {
        if (params.cardId) {
          this.proceedCardAdditionFlow(params.cardId);
        }
      });
  }

  ngOnInit() {
    this._userService.currentUser
      .subscribe(user => {
        this.user = user;
        this.onLoading();
      });

    this._apiService.getFiatCards()
      .subscribe(cards => {
        this.cards = cards.data;
        this.onLoading();
      });
  }

  onLoading() {
    if (this.user && this.user.id && this.cards) {
      this.loading = false;
      this._cdRef.detectChanges();
    }
  }

  navigateToCardStatus(cardId: number) {
    this._router.navigate(['/account/cards/' + cardId]);
  }

  navigateToCardList() {
    this._router.navigate(['/account/cards']);
  }

  goto(page: Page) {
    this.page = page;
  }

  addCard() {
    this.buttonBlur.emit();
    this.processing = true;

    this._apiService.addFiatCard(window.location.origin + '/#/account/cards/:cardid')
      .finally(() => {
        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
      res => {
        window.location.href = res.data.redirect;
      },
      err => { });
  }

  removeCard(card) {
    this._translate.get('MessageBox.Remove').subscribe(phrase => {
      this._messageBox.confirm(phrase).subscribe(ok => {
        if (ok) {
          this.processing = true;
          this._apiService.removeFiatCard(card.cardId)
            .finally(() => {
              this.processing = false;
              this._cdRef.markForCheck();
            }).subscribe(() => {
            const id = this.cards.list.indexOf(card);
            id >= 0 && this.cards.list.splice(id, 1);
            this._translate.get('MessageBox.Removed').subscribe(phrase => {
              this._messageBox.alert(phrase);
            });
          }, err => {
              this._messageBox.alert(err.error.errorDesc);
            }
          );
        }
      });
    });
  }

  proceedCardAdditionFlow(cardId: number) {
    this.processing = true;
    this.currentCardId = cardId;

    this._apiService.getFiatCardStatus(cardId)
      .finally(() => {
        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
      (res: APIResponse<CardStatusResponse>) => {
        switch (res.data.status) {
          case 'initial':
            this.goto(Page.OnNew);
            break;

          case 'confirm':
            this.goto(Page.OnNeedConfirmation);
            break;

          case 'verification':
            this.goto(Page.OnNeedVerification);
            break;

          case 'verified':
            this.goto(Page.OnVerified);
            break;

          case 'failed':
            this.goto(Page.OnFailure);
            break;

          // case 'payment':
          //   this._messageBox.alert('Pending test payment completion...<br>You can continue adding your cards.');
          //   break;
        }
      },
      err => { });
  }

  confirmCard(cardId: number) {
    this.buttonBlur.emit();
    this.processing = true;

    this._apiService.confirmFiatCard(cardId, window.location.origin + '/#/account/cards/:cardid')
      .finally(() => {
        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
      res => {
        window.location.href = res.data.redirect;
      },
      err => {
        if (err.error && err.error.errorCode) {
          switch (err.error.errorCode) {
            case 100:
              this._translate.get('MessageBox.WrongID').subscribe(phrase => {
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

  verifyCard(cardId: number) {
    this.buttonBlur.emit();

    if (this.verificationCode !== null && !isNaN(this.verificationCode)) {
      this.processing = true;

      this._apiService.verifyFiatCard(cardId, this.verificationCode)
        .finally(() => {
          this.processing = false;
          this._cdRef.detectChanges();
        })
        .subscribe(
        res => {
          this.goto(Page.OnVerified);
          this.loading = true;

          this._apiService.getFiatCards()
            .finally(() => {
              this.loading = false;
              this._cdRef.detectChanges();
            })
            .subscribe(
            res => {
              this.cards = res.data;
            },
            err => { });
        },
        err => {
          if (err.error && err.error.errorCode) {
            switch (err.error.errorCode) {
              case 100:
                this._translate.get('MessageBox.InvalidCode').subscribe(phrase => {
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
    else {
      this._translate.get('MessageBox.InvalidFormat').subscribe(phrase => {
        this._messageBox.alert(phrase);
      });
    }
  }

}
