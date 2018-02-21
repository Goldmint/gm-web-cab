import {ChangeDetectorRef, Component, OnInit, OnDestroy, ViewChild} from '@angular/core';
import {APIService, MessageBoxService, UserService} from "../../../services";
import {TransparencyRecord, TransparencySummary} from "../../../interfaces";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {Page} from "../../../models/page";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import {Subscription} from "rxjs/Subscription";

@Component({
  selector: 'app-oplog-page',
  templateUrl: './oplog-page.component.html',
  styleUrls: ['./oplog-page.component.sass']
})
export class OplogPageComponent implements OnInit, OnDestroy {

  public locale: string;
  public loading: boolean;
  public page = new Page();

  public summary: TransparencySummary;

  public rows:  Array<TransparencyRecord> = [];
  public subRows:  Array<TransparencyRecord> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No Data'};

  public form: FormGroup;

  public currentUser = {};
  public showOplog = false;
  private filterValue = '';
  public sub1: Subscription;
  public isSubStepsShowing: boolean = false;

  @ViewChild('formDir') formDir;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private formBuilder: FormBuilder,
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

    this.sub1 = this.userService.oplogTransferData$.subscribe((data) => {
      this.currentUser = {id: data['id'], user: data['user']};
      this.setPage({ offset: 0 });
    })

  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 }, this.filterValue);
  }

  setPage(pageInfo, filter = '') {
    this.showOplog = true;
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getOplog(this.currentUser['id'], filter, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
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

  oplogFilter() {
    this.filterValue = this.form.controls.filter.value;
    this.setPage({ offset: 0 }, this.filterValue);
  }

  close() {
    if (this.isSubStepsShowing) {
      this.isSubStepsShowing = false;
    } else {
      this.showOplog = false;
      this.userService.oplotCloseModal(true);
    }
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }

  showSubItems(row) {
    this.subRows = row.steps;
    this.subRows.unshift(row);
    this.isSubStepsShowing = true;
    this.cdRef.detectChanges();
  }

}