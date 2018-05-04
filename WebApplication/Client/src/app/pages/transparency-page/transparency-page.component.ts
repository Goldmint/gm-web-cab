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
  public isMobile: boolean;

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
    this.isMobile = (window.innerWidth <= 576);
    window.onresize = () => {
      this.isMobile = window.innerWidth <= 576 ? true : false;
      this.cdRef.markForCheck();
    };

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.PAGES.History.Table.EmptyMessage;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.summary = {
      issued:      {"amount": 0, "suffix": " GOLD"},
      burnt:       {"amount": 0, "suffix": " GOLD"},
      circulation: {"amount": 0, "suffix": " GOLD"}
    };

    this._ethService.getObservableTotalGoldBalances().subscribe(data => {
      if (data) {
        this.summary.issued.amount = data['issued'].div(new BigNumber(10).pow(18)).toFixed(2);
        this.summary.burnt.amount = data['burnt'].div(new BigNumber(10).pow(18)).toFixed(2);
        this.summary.circulation.amount = +(this.summary.issued.amount - this.summary.burnt.amount).toFixed(2);
        this.cdRef.detectChanges();
      }
   });

    this.setPage({ offset: 0 });
    this.cdRef.markForCheck();
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

          ['assets', 'bonds', 'fiat', 'gold'].forEach(field => {
            let object = {};
            this.statData[field].forEach(item => {
              object[item.k] = item.v;
            });
            this.statData[field] = object;
          });

          this.statData['viewDataTimestamp'] = this.statData['dataTimestamp']
            ? this.datePipe.transform(this.statData['dataTimestamp'] * 1000, 'MMM d, y') : '-';
          this.statData['viewAuditTimestamp'] = this.statData['auditTimestamp']
            ? this.datePipe.transform(this.statData['auditTimestamp'] * 1000, 'MMM d, y') : '-';

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
