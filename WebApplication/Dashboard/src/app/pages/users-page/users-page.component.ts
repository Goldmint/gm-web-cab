import {ChangeDetectorRef, Component, OnInit, ViewChild} from '@angular/core';
import {APIService, MessageBoxService, UserService} from "../../services";
import {TransparencyRecord, TransparencySummary} from "../../interfaces";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {Page} from "../../models/page";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import 'rxjs/add/operator/debounceTime';

@Component({
  selector: 'app-users-page',
  templateUrl: './users-page.component.html',
  styleUrls: ['./users-page.component.sass']
})
export class UsersPageComponent implements OnInit {

  public locale: string;
  public loading: boolean;
  public page = new Page();

  public summary: TransparencySummary;

  public rows:  Array<TransparencyRecord> = [];
  public sorts: Array<any> = [{prop: 'id', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No Data'};

  public form: FormGroup;

  public showModal = false;
  public showAccountInfo = false;
  public currentUser = {};

  private filterValue = '';

  @ViewChild('formDir') formDir;

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
    this.form = this.formBuilder.group({
      'filter': ['', Validators.required]
    });

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.PAGES.History.Table.EmptyMessage;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.setPage({ offset: 0 }, this.filterValue);
    this.userService.oplotCloseModal$.subscribe(() => {
      this.showAccountInfo = true;
      this.cdRef.detectChanges();
    });
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 }, this.filterValue);
  }

  setPage(pageInfo, filter) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getUsersList(filter, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .subscribe(
        data => {
          this.rows = data.data.items;

          this.page.totalElements = data.data.total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

          this.loading = false;
          this.cdRef.detectChanges();

          const tableTitle = document.getElementById('pageSectionTitle');
          if (tableTitle && tableTitle.getBoundingClientRect().top < 0) {
            tableTitle.scrollIntoView();
          }
        });
  }

  userFilter() {
    this.filterValue = this.form.controls.filter.value;
    this.setPage({ offset: 0 }, this.filterValue);
  }

  showUserInfo(row) {
    this.showModal = true;
    this.showAccountInfo = true;

    this.currentUser = row;
    this.currentUser['timeRegistered'] = new Date(row.timeRegistered);
  }

  showOplogInfo(id: number, user: string) {
    this.userService.oplogTransferData({id, user});

    this.showAccountInfo = false;
    this.cdRef.detectChanges();
  }

  close() {
    this.showModal = false;
    this.showAccountInfo = false;
  }

}
