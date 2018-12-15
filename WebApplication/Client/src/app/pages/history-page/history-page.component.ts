import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef,
  OnDestroy
} from '@angular/core';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';
import { Page } from '../../models/page';
import { HistoryRecord } from '../../interfaces';
import {UserService, APIService} from '../../services';
import {Subject} from "rxjs/Subject";
import {Observable} from "rxjs/Observable";
import {Subscription} from "rxjs/Subscription";
import {environment} from "../../../environments/environment";


@Component({
  selector: 'app-history-page',
  templateUrl: './history-page.component.html',
  styleUrls: ['./history-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HistoryPageComponent implements OnInit, OnDestroy {
  public locale: string;
  public loading: boolean = true;
  public page = new Page();

  public rows:  Array<HistoryRecord> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No data'};
  public etherscanUrl = environment.etherscanUrl;
  public isMobile: boolean;
  public isAuthenticated: boolean = false;

  private destroy$: Subject<boolean> = new Subject<boolean>();
  private interval: Subscription;

  constructor(
    private userService: UserService,
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService
  ) {

    this.page.pageNumber = 0;
    this.page.size = 10;
  }

  ngOnInit() {
    this.isAuthenticated = this.userService.isAuthenticated();
    !this.isAuthenticated && (this.loading = false);

    this.isMobile = (window.innerWidth <= 767);

    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(size => {
      this.isMobile = size <= 767 ? true : false;
      this.cdRef.markForCheck();
    });

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.PAGES.History.Table.EmptyMessage;
    });

    this.userService.currentLocale.takeUntil(this.destroy$).subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.isAuthenticated && this.setPage({ offset: 0 });
    this.cdRef.markForCheck();
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getHistory(this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .subscribe(
        res => {
          this.interval && this.interval.unsubscribe();
          this.rows = res.data.items;

          this.page.totalElements = res.data.total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);
          this.loading = false;
          this.interval = Observable.interval(30000).subscribe(() => {
            this.setPage({ offset: this.page.pageNumber });
          });
          this.cdRef.markForCheck();

          const tableTitle = document.getElementById('pageSectionTitle');
          if (tableTitle && tableTitle.getBoundingClientRect().top < 0) {
            tableTitle.scrollIntoView();
          }
        });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    this.interval && this.interval.unsubscribe();
  }

}
