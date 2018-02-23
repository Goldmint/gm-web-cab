import {ChangeDetectorRef, Component, OnInit, OnDestroy, ViewChild, ViewEncapsulation} from '@angular/core';
import {APIService, UserService} from "../../../services";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {Page} from "../../../models/page";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import {Subscription} from "rxjs/Subscription";
import {ActivatedRoute} from "@angular/router";

@Component({
  selector: 'app-oplog-page',
  templateUrl: './oplog-page.component.html',
  styleUrls: ['./oplog-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class OplogPageComponent implements OnInit, OnDestroy {

  public locale: string;
  public loading: boolean;
  public page = new Page();

  public rows:  Array<any> = [];
  public subRows:  Array<any> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No Data'};

  public form: FormGroup;

  public currentUser = {};
  private filterValue = '';
  public sub1: Subscription;
  public isSubStepsShowing: boolean = false;
  public userId;

  @ViewChild('formDir') formDir;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private formBuilder: FormBuilder,
    private route: ActivatedRoute
  ) {

    this.page.pageNumber = 0;
    this.page.size = 5;
  }

  ngOnInit() {

    this.form = this.formBuilder.group({
      'filter': ['', Validators.required]
    });

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.NoData;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.sub1 = this.route.params.subscribe(params => {
      this.userId = params.id;
      this.setPage({offset: 0});
    });

  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getOplog(this.userId, this.filterValue, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
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
    this.setPage({ offset: 0 });
  }

  closeSubSteps() {
    this.isSubStepsShowing = false;
  }

  showSubItems(row) {
    this.subRows = [row];
    this.subRows.push.apply(this.subRows, row.steps);
    this.isSubStepsShowing = true;
    this.cdRef.detectChanges();
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }

}