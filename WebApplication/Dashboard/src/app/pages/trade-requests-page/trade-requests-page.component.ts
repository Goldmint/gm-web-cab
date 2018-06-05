import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewChild} from '@angular/core';
import {APIService, MessageBoxService, UserService} from "../../services";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {Page} from "../../models/page";
import {BigNumber} from "bignumber.js";
import {TransparencySummary} from "../../interfaces/transparency-summary";
import {EthereumService} from "../../services/ethereum.service";
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-trade-requests-page',
  templateUrl: './trade-requests-page.component.html',
  styleUrls: ['./trade-requests-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TradeRequestsPageComponent implements OnInit {

  @ViewChild('searchByDateForm') searchByDateForm

  public locale: string;
  public loading: boolean;
  public isDataLoaded = false;
  public invalidDate = false;
  public page = new Page();
  public viewDate = {
    start: null,
    end: null
  };

  public rows:  Array<any> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No Data'};
  public summary: TransparencySummary;

  public filterRequestId = null;
  public periodStart = null;
  public periodEnd = null;
  public degree = Math.pow(10, -18);
  public etherscanUrl = environment.etherscanUrl;

  constructor(
    private apiService: APIService,
    private ethService: EthereumService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private _messageBox: MessageBoxService
  ) {

    this.page.pageNumber = 0;
    this.page.size = 5;
  }

  ngOnInit() {
    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.NoData;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.summary = {
      issued:      {"amount": 0, "suffix": " GOLD"},
      burnt:       {"amount": 0, "suffix": " GOLD"},
      circulation: {"amount": 0, "suffix": " GOLD"}
    };

    this.setPage({ offset: 0 });
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  searchByDate() {
    this.setPage({ offset: 0 });
  }

  resetSearchForm() {
    this.searchByDateForm.reset();
    this.filterRequestId = this.periodStart = this.periodEnd = this.viewDate.start = this.viewDate.end = null;
    this.setPage({ offset: 0 });
    this.cdRef.markForCheck();
  }

  onDateChange(value: Date, isStart: boolean) {
    this.invalidDate = false;

    if (value !== null) {
      if (isStart) {
        this.viewDate.start = value;
        this.periodStart = Date.parse(value.toString()) / 1000;
      } else {
        this.viewDate.end = value;
        this.periodEnd = Date.parse(value.toString()) / 1000;
      }

      if (this.periodStart && this.periodEnd && this.periodStart > this.periodEnd) {
        this.invalidDate = true;
      }
    }
    this.cdRef.markForCheck();
  }

  getTotalCirculation() {
    this.ethService.getObservableTotalGoldBalances().subscribe(data => {
      if (data) {
        this.summary.issued.amount = +data['issued'].div(new BigNumber(10).pow(18)).toFixed(2);
        this.summary.burnt.amount = +data['burnt'].div(new BigNumber(10).pow(18)).toFixed(2);
        this.summary.circulation.amount = +(this.summary.issued.amount - this.summary.burnt.amount).toFixed(2);
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      }
    });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.goldExchangeList(this.filterRequestId, this.periodStart, this.periodEnd, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .finally(() => {
        this.loading = false;
        this.cdRef.markForCheck();
      })
      .subscribe(
        data => {
          this.rows = data.data.items;

          if (data.data.totalBurnt === null || data.data.totalIssued === null) {
            this.getTotalCirculation();
          } else {
            this.summary.issued.amount = new BigNumber(data.data.totalIssued);
            this.summary.burnt.amount = new BigNumber(data.data.totalBurnt);
            this.summary.circulation.amount = +this.summary.issued.amount.minus(this.summary.burnt.amount).toFixed(2);
            this.isDataLoaded = true;
            this.cdRef.markForCheck();
          }

          this.page.totalElements = data.data.total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

          const tableTitle = document.getElementById('pageSectionTitle');
          if (tableTitle && tableTitle.getBoundingClientRect().top < 0) {
            tableTitle.scrollIntoView();
          }
        }, () => {
          this._messageBox.alert('Error. Something went wrong.');
        });
  }
}

