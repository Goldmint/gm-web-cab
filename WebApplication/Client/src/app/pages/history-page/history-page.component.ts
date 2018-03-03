import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';

import { Page } from '../../models/page';
import { HistoryRecord } from '../../interfaces';
import {UserService, APIService, EthereumService} from '../../services';

@Component({
  selector: 'app-history-page',
  templateUrl: './history-page.component.html',
  styleUrls: ['./history-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HistoryPageComponent implements OnInit {
  public locale: string;
  public loading: boolean = true;
  public page = new Page();

  public rows:  Array<HistoryRecord> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No data'};

  constructor(
    private userService: UserService,
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private _ethService: EthereumService,) {

    this.page.pageNumber = 0;
    this.page.size = 10;
  }

  ngOnInit() {
    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.PAGES.History.Table.EmptyMessage;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.setPage({ offset: 0 });
    this._ethService.getObservableUsdBalance().subscribe(() => {
      this.setPage({ offset: this.page.pageNumber });
    });
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
          this.rows = res.data.items;

          this.page.totalElements = res.data.total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);
          this.loading = false;
          this.cdRef.detectChanges();

          const tableTitle = document.getElementById('pageSectionTitle');
          if (tableTitle && tableTitle.getBoundingClientRect().top < 0) {
            tableTitle.scrollIntoView();
          }
        });
  }

}
