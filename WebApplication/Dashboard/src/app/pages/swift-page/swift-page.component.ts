import {ChangeDetectorRef, Component, OnInit, ViewChild, ViewEncapsulation} from '@angular/core';
import {APIService, MessageBoxService, UserService} from "../../services";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {Page} from "../../models/page";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";

enum Tables { Deposit, Withdraw }

@Component({
  selector: 'app-swift-page',
  templateUrl: './swift-page.component.html',
  styleUrls: ['./swift-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class SwiftPageComponent implements OnInit {

  @ViewChild('amount') amount;
  @ViewChild('comment') comment;
  @ViewChild('refuseComment') refuseComment;

  public tables = Tables;
  public currentTable: any;

  public locale: string;
  public loading: boolean;
  public progress: boolean;
  public page = new Page();
  public isExclude = false;
  public showModal: boolean = false;

  public rows = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No Data'};

  public dataType: number = 1;
  public currentId: number;
  public userInfo: object;
  public isRefuse = false;


  public form: FormGroup;
  private filterValue = '';

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private formBuilder: FormBuilder,
    private _messageBox: MessageBoxService
  ) {

    this.page.pageNumber = 0;
    this.page.size = 5;
  }

  ngOnInit() {
    this.currentTable = this.tables.Deposit;
    this.form = this.formBuilder.group({
      'filter': ['', Validators.required]
    });

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.NoData;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.setPage({ offset: 0 });
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  excludeCompleted(exclude) {
    this.isExclude = exclude.checked;
    this.setPage({ offset: this.page.pageNumber });
    this.cdRef.detectChanges();
  }

  setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getSwiftList(this.filterValue, this.isExclude, this.dataType, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir).subscribe(data => {
      this.rows = data.data.items;

      this.page.totalElements = data.data.total;
      this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

      this.loading = false;
      this.cdRef.detectChanges();
    });
  }

  swiftFilter() {
    this.filterValue = this.form.controls.filter.value;
    this.setPage({ offset: 0 });
  }

  changeTable(table) {
    if (this.currentTable !== table) {
      this.currentTable = table
      this.dataType = this.currentTable + 1;
      this.setPage({ offset: 0 });
      this.cdRef.detectChanges();
    }
  }

  lock(id) {
    this.loading = true;
    this.isRefuse = false;
    this.userInfo = {};
    this.currentId = id;
    if (this.currentTable === this.tables.Deposit) {
      this.apiService.swiftLockDeposit(id)
        .finally(() => {
          this.loading = false;
          this.cdRef.detectChanges();
        }).subscribe(data => {
          this.userInfo = data.data.user;
          this.showModal = true;
        },
        error => {
          this._messageBox.alert(error.error.errorDesc);
        }
      )
    } else if (this.currentTable === this.tables.Withdraw) {
      this.apiService.swiftLockWithdraw(id)
        .finally(() => {
          this.loading = false;
          this.cdRef.detectChanges();
        }).subscribe(data => {
          this.userInfo = data.data.user;
          this.showModal = true;
        },
        error => {
          this._messageBox.alert(error.error.errorDesc);
        }
      )
    }
  }

  accept() {
    this.progress = true;
    if (this.currentTable === this.tables.Deposit) {
      this.apiService.swiftAcceptDeposit(this.currentId, this.amount.value, this.comment.value)
        .finally(() => {
          this.progress = false;
          this.cdRef.detectChanges();
        }).subscribe(() => {
        this.showModal = false;
        this._messageBox.alert('accepted');
        this.cdRef.detectChanges();
      },
        error => {
          this._messageBox.alert(error.error.errorDesc);
        });
    } else if (this.currentTable === this.tables.Withdraw) {
      // accept withdraw
      this.showModal = false;
      this.progress = false;
      this.cdRef.detectChanges();
    }
  }

  refuse() {
    this.progress = true;
    if (this.currentTable === this.tables.Deposit) {
      this.apiService.swiftRefuseDeposit(this.currentId, this.refuseComment.value)
        .finally(() => {
          this.progress = false;
          this.cdRef.detectChanges();
        }).subscribe(() => {
          this.showModal = false;
          this._messageBox.alert('refused');
        },
        error => {
          this._messageBox.alert(error.error.errorDesc);
        });
    } else if (this.currentTable === this.tables.Withdraw) {
      this.apiService.swiftRefuseWithdraw(this.currentId, this.refuseComment.value)
        .finally(() => {
          this.progress = false;
          this.cdRef.detectChanges();
        }).subscribe(() => {
          this.showModal = false;
          this._messageBox.alert('refused');
        },
        error => {
          this._messageBox.alert(error.error.errorDesc);
        });
    }
  }

}
