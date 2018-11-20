import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';
import {APIService, UserService} from "../../../../../services";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../../../models/page";
import {FeedList} from "../../../../../interfaces/feed-list";
import {PawnshopDetails} from "../../../../../interfaces/pawnshop-details";
import {TranslateService} from "@ngx-translate/core";
import 'anychart';
import {CommonService} from "../../../../../services/common.service";
import {OrgStepperData} from "../../../../../models/org-stepper-data";

@Component({
  selector: 'app-feed-table',
  templateUrl: './feed-table.component.html',
  styleUrls: ['./feed-table.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FeedTableComponent implements OnInit {

  public pawnshopId: number;
  public page = new Page();
  public rows: FeedList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public loading: boolean = false;
  public isMobile: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = 0;
  public pawnshopDetails: PawnshopDetails;
  public rate: number;
  public rateChartData = [];

  private rateChart: any = {};
  private paginationHistory: number[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private translate: TranslateService,
    private commonService: CommonService
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(width => {
      this.isMobile = width <= 992;
      this.cdRef.markForCheck();
    });

    this.userService.currentLocale.takeUntil(this.destroy$).subscribe(() => {
      if (this.isDataLoaded) {
        this.translate.get('PAGES.Pawnshop.Feed.PawnshopDetails.Charts.Rate').subscribe(phrase => {
          this.rateChart.chart.title(phrase);
        });
      }
    });

    this.commonService.organizationStepper$.takeUntil(this.destroy$).subscribe((data: OrgStepperData) => {
      if (data !== null && data.step === 3) {
        this.paginationHistory = [];
        this.pawnshopId = data.id;

        this.setPage(this.pawnshopId, null, true);
        this.getPawnshopDetails(this.pawnshopId);
      }
    });
  }

  getPawnshopDetails(id) {
    this.apiService.getPawnshopDetails(id).subscribe((data: any) => {
      this.pawnshopDetails = data.res;
      this.rate = this.pawnshopDetails.daily_stats[0].currently_opened_amount;
      this.setChartsData(this.pawnshopDetails.daily_stats);
      this.initDailyStatChart();

      this.isDataLoaded = true;
      this.cdRef.markForCheck();
    });
  }

  setChartsData(res) {
    if (res) {
      res.forEach(item => {
        const date = new Date(item.time * 1000);
        let month = (date.getMonth()+1).toString(),
          day = date.getDate().toString();

        month.length === 1 && (month = '0' + month);
        day.length === 1 && (day = '0' + day);

        const dateString = date.getFullYear() + '-' + month + '-' + day;
        this.rateChartData.push([dateString, +item.currently_opened_amount]);
      });
    }
  }

  initDailyStatChart() {
    if (this.rateChart.hasOwnProperty('table')) {
      this.rateChart.table.remove();
      this.rateChart.table.addData(this.rateChartData);
      return
    }

    anychart.onDocumentReady( () => {
      this.rateChart.table = anychart.data['table']();
      this.rateChart.table.addData(this.rateChartData);

      this.rateChart.mapping = this.rateChart['table'].mapAs();
      this.rateChart.mapping.addField('value', 1);

      this.rateChart.chart = anychart.stock();
      this.rateChart.chart.plot(0).line(this.rateChart.mapping).name('Volume (GOLD)');
      this.rateChart.chart.plot(0).legend().itemsFormatter(() => {
        return [
          {text: "Volume", iconFill:"#63B7F7"}
        ]
      });

      this.translate.get('PAGES.Pawnshop.Feed.PawnshopDetails.Charts.Rate').subscribe(phrase => {
        this.rateChart.chart.title(phrase);
      });
      this.rateChart.chart.container('daily-stats-chart-container');
      this.rateChart.chart.draw();
    });
  }

  setPage(org: number, from: number = null, isNext: boolean = true) {
    this.loading = true;

    this.apiService.getPawnList(org, from >= 0 ? from : null)
      .finally(() => {
        this.loading = false;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        this.rows = data.res.list ? data.res.list : [];

        if (this.rows.length) {
          if (!isNext) {
            this.paginationHistory.pop();
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = +this.rows[this.rows.length - 1].id);
          }
          isNext && this.paginationHistory.push(+this.rows[this.rows.length - 1].id);
        } else {
          isNext && this.paginationHistory.push(null);
        }

        (!this.rows.length || (this.offset === 0 && this.rows.length < 10)) && (this.isLastPage = true);
      });
  }

  prevPage() {
    this.offset--;
    this.setPage(this.pawnshopId, this.paginationHistory[this.paginationHistory.length - 3], false);
  }

  nextPage() {
    this.offset++;
    this.setPage(this.pawnshopId, this.paginationHistory[this.paginationHistory.length - 1], true);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
