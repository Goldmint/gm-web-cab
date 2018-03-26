import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';

import { Page } from '../../models/page';
import { TransparencySummary, TransparencyRecord } from '../../interfaces';
import {APIService, EthereumService, UserService} from '../../services';
import {BigNumber} from "bignumber.js";
import {DatePipe} from "@angular/common";

@Component({
  selector: 'app-transparency-page',
  templateUrl: './transparency-page.component.html',
  styleUrls: ['./transparency-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransparencyPageComponent implements OnInit {
  public locale: string;
  public loading: boolean;
  public isDataLoaded = false;
  public page = new Page();

  public summary: TransparencySummary;

  public rows:  Array<TransparencyRecord> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No data'};
  public statData: object;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private _ethService: EthereumService,
    private datePipe: DatePipe) {

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
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getTransparency(this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .subscribe(
        res => {
          this.rows = res.data.items;

          this.statData = res.data.stat;
          this.statData['viewDataTimestamp'] = this.datePipe.transform(this.statData['dataTimestamp'] * 1000, 'dd.MM.yy');
          this.statData['viewAuditTimestamp'] = this.datePipe.transform(this.statData['auditTimestamp'] * 1000, 'dd.MM.yy');

          this.page.totalElements = res.data.total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

          this.loading = false;
          this.isDataLoaded = true;

          const tableTitle = document.getElementById('pageSectionTitle');
          if (tableTitle && tableTitle.getBoundingClientRect().top < 0) {
            tableTitle.scrollIntoView();
          }
          this.cdRef.detectChanges();
        });
  }

}
