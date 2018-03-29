import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter,
  ViewChild
} from '@angular/core';
import { Observable } from "rxjs/Observable";

import { TFAInfo, CardsList, CardsListItem, FiatLimits } from '../../interfaces';
import {APIService, EthereumService, MessageBoxService} from '../../services';
import { UserService } from "../../services/user.service";
import { User } from "../../interfaces/user";
import {TranslateService} from "@ngx-translate/core";
import {BigNumber} from "bignumber.js";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";

enum Pages { Default, CardsList, CardsListSuccess, BankTransfer, CryptoCapital }

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
  public addBankForm: FormGroup;
  public bankTransferForm: FormGroup;

  public page: Pages;
  public loading: boolean = true;
  public processing: boolean = false;
  public isDataLoaded: boolean = false;
  public tfaInfo: TFAInfo;
  public cards: CardsList;

  public limitsIncrease: boolean = true; //@todo: dev

  public depositModel: any = {};
  public buttonBlur = new EventEmitter<boolean>();
  public errors = [];
  public limits: FiatLimits;
  public user: User;
  public bankTransferList: Array<any>;
  public swiftWithdrawChecked:boolean = false;
  public cardWithdrawChecked:boolean = false;

  public minAmount: number;
  public maxAmount: number;
  public bankTransferBlock = ['transfer', 'addAccount', 'removeAccount'];
  public currentTransferBlock = this.bankTransferBlock[0];
  public bankTemplateIdForRemove: number;

  public userUsdBalance: number;
  public usdAmount;
  public coinList = ['btc', 'eth']
  public currentCoin = this.coinList[1];
  public estimatedAmount = 0;
  public maxBankTransferAmount: number;

  constructor(
    private _apiService: APIService,
    private _ethService: EthereumService,
    private _cdRef: ChangeDetectorRef,
    private _user: UserService,
    private _messageBox: MessageBoxService,
    private _translate: TranslateService,
    private formBuilder: FormBuilder
  ) {
    this.page = Pages.Default;
  }

  ngOnInit() {

    Observable.zip(
      this._apiService.getTFAInfo(),
      this._apiService.getLimits(),
      this._user.currentUser,
      this._apiService.getSwiftWithdrawTemplatesList()
    ).subscribe(res => {
      this.tfaInfo = res[0].data;
      this.limits = res[1].data;
      this.user = res[2];
      this.bankTransferList = res[3].data.list;

      this._ethService.getObservableUsdBalance().subscribe(balance => {
        this.userUsdBalance = balance;
        this.maxBankTransferAmount = Math.min(this.userUsdBalance, this.limits.paymentMethod.swift.withdraw.max,
          this.limits.current.withdraw.day);

        this.bankTransferForm = this.formBuilder.group({
          'bankTransferAmount': ['', [Validators.min(this.limits.paymentMethod.swift.withdraw.min),
            Validators.max(this.maxBankTransferAmount)]],
          'selectBankAccount': ['', [Validators.required]]
        });

        this.loading = false;
        this._cdRef.markForCheck();
      });

      this.minAmount = this.limits.paymentMethod.card.withdraw.min;
      this.maxAmount = this.limits.current.withdraw.minimal;
      this.loading = false;
      this.isDataLoaded = true;
      this._cdRef.markForCheck();
    });

    this.addBankForm = this.formBuilder.group({
      'accountName': ['', [Validators.required]],
      'address': ['', [Validators.required]],
      'iban': ['', [Validators.required]],
      'bankName': ['', [Validators.required]],
      'bic': ['', [Validators.required]],
      'description': ['', [Validators.required]]
    });

    this._cdRef.markForCheck();
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

      case Pages.BankTransfer:
        this.page = page;
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

  chooseCurrentCoin(coin) {
    if (this.currentCoin !== coin) {
      this.currentCoin = coin;
    }
  }

  onCryptoCurrencyChanged(value) {
    if (value != null && value > 0) {
      this.usdAmount = (new BigNumber(value)).decimalPlaces(2, BigNumber.ROUND_DOWN);
      this.estimatedAmount = +(this.usdAmount / 538.94).toFixed(6);
    } else {
      this.estimatedAmount = 0;
    }
  }

  addBankAccount() {
    const data = {
      "name": this.addBankForm.get('accountName').value,
      "holder": this.addBankForm.get('address').value,
      "iban": this.addBankForm.get('iban').value.toString(),
      "bank": this.addBankForm.get('bankName').value,
      "bic": this.addBankForm.get('bic').value.toString(),
      "details": this.addBankForm.get('description').value
    }
    this.loading = true;
    this._apiService.addSwiftWithdrawTemplate(data)
      .finally(() => {
        this.loading = false;
        this._cdRef.detectChanges();
      }).subscribe(() => {
      this._apiService.getSwiftWithdrawTemplatesList().subscribe((data) => {
        this.bankTransferList = data.data.list;
        this._messageBox.alert('Account has been added');
        this.addBankForm.reset();
        this.currentTransferBlock = this.bankTransferBlock[0];
        this._cdRef.detectChanges();
      });
    });
  }

  removeBankAccount() {
    this._messageBox.confirm('Do you want to remove account?').subscribe(ok => {
      if (ok) {
        this.loading = true;
        this._apiService.removeSwiftWithdrawTemplate(+this.bankTemplateIdForRemove)
          .finally(() => {
            this.loading = false;
            this._cdRef.detectChanges();
          })
          .subscribe(() => {
          this.bankTransferList.forEach((item, index) => {
            if (item.templateId == +this.bankTemplateIdForRemove) {
              this.bankTransferList.splice(index, 1);
              this.bankTemplateIdForRemove = null;
              this._messageBox.alert('Account has been removed');
            }
          });
        });
        this._cdRef.detectChanges();
      }
    });
  }

  bankTransferSendRequest() {
    const amount = +this.bankTransferForm.get('bankTransferAmount').value;
    const account = +this.bankTransferForm.get('selectBankAccount').value;

    this.loading = true;
    this._apiService.getSwiftWithdrawInvoice(amount, account)
      .finally(() => {
        this.loading = false;
        this._cdRef.detectChanges();
      })
      .subscribe(() => {
      this._messageBox.alert('Request has been sent');
      this.bankTransferForm.reset();
    });
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
