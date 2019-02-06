import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';
import {APIService} from "../../../../services/index";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../../models/page";
import {PawnshopList} from "../../../../interfaces/pawnshop-list";
import {CommonService} from "../../../../services/common.service";
import {ActivatedRoute, Router} from "@angular/router";
import {TranslateService} from "@ngx-translate/core";
import {UserService} from "../../../../services";

@Component({
  selector: 'app-pawnshops-table',
  templateUrl: './pawnshops-table.component.html',
  styleUrls: ['./pawnshops-table.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PawnshopsTableComponent implements OnInit {

  public orgId: number;
  public page = new Page();
  public rows: PawnshopList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = -1;
  public selected: PawnshopList[] = [];
  public orgName: string;
  public orgChartData = [];

  private orgChart: any = {};
  private paginationHistory: number[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private commonService: CommonService,
    private translate: TranslateService,
    private userService: UserService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.route.params.takeUntil(this.destroy$).subscribe(params => {
      this.orgId = params.id;
      this.setPage(params.id, null, true, true);
      this.apiService.getOrganizationDetails(this.orgId).subscribe((data: any) => {
        this.setChartsData(data.res.daily_stats);
        this.initDailyStatChart();
      });
    });
  }

  ngOnInit() {
    this.userService.currentLocale.takeUntil(this.destroy$).subscribe(() => {
      if (this.isDataLoaded) {
        this.translate.get('PAGES.Pawnshop.Feed.Charts.OrgChart').subscribe(phrase => {
          this.orgChart.chart.title(phrase);
        });
      }
    });
  }

  setPage(org: number, from: number = null, isNext: boolean = true, isRouteChange: boolean = false) {
    this.loading = true;

    this.apiService.getPawnshopList(org, from >= 0 ? from : null)
      .finally(() => {
        this.loading = false;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        if (data.res.list && data.res.list.length) {
          this.rows = data.res.list;
        }

        if (isRouteChange) {
          this.rows.length ? this.getOrganizationName(this.rows[0].org_id) : this.orgName = '-';
          this.cdRef.markForCheck();
        }

        if (data.res.list && data.res.list.length) {
          if (!isNext) {
            this.offset--;
            this.paginationHistory.pop();
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = +this.rows[this.rows.length - 1].id);
          } else {
            this.offset++;
            this.paginationHistory.push(+this.rows[this.rows.length - 1].id);
          }
        }

        if (!data.res.list || (data.res.list && !data.res.list.length)) {
          this.isLastPage = true;
        }
      });
  }

  getOrganizationName(orgId: number) {
    this.commonService.getPawnShopOrganization.takeUntil(this.destroy$).subscribe((orgList: any) => {
      if (orgList) {
        for (let key in orgList) {
          if (orgId === +key) {
            this.orgName = orgList[key];
            this.cdRef.markForCheck();
          }
        }
      }
    });
  }

  prevPage() {
    this.setPage(this.orgId, this.paginationHistory[this.paginationHistory.length - 3], false, false);
  }

  nextPage() {
    this.setPage(this.orgId, this.paginationHistory[this.paginationHistory.length - 1], true, false);
  }

  back() {
    this.router.navigate(['/pawnshop-loans/feed/organizations']);
  }

  onSelect({ selected }) {
    this.router.navigate(['/pawnshop-loans/feed/organization-feed/', selected[0].id]);
  }

  setChartsData(res) {
    if (res) {
      this.orgChartData = [];
      res.forEach(item => {
        const date = new Date(item.time * 1000);
        let month = (date.getMonth()+1).toString(),
          day = date.getDate().toString();

        month.length === 1 && (month = '0' + month);
        day.length === 1 && (day = '0' + day);

        const dateString = date.getFullYear() + '-' + month + '-' + day;
        this.orgChartData.push([dateString, +item.currently_opened_amount]);
      });
    }
  }

  initDailyStatChart() {
    let isDataNotEmpty = false;
    this.orgChartData.forEach(item => {
      item[1] > 0 && (isDataNotEmpty = true);
    });
    !isDataNotEmpty && (this.orgChartData = []);

    if (this.orgChart.hasOwnProperty('table')) {
      this.orgChart.table.remove();
      this.orgChart.table.addData(this.orgChartData);
      this.checkNoDataChart();
      return
    }

    anychart.onDocumentReady( () => {
      this.orgChart.table = anychart.data['table']();
      this.orgChart.table.addData(this.orgChartData);

      this.orgChart.mapping = this.orgChart['table'].mapAs();
      this.orgChart.mapping.addField('value', 1);

      this.orgChart.chart = anychart.stock();
      this.orgChart.chart.plot(0).line(this.orgChart.mapping).name('Volume (GOLD)');
      this.orgChart.chart.plot(0).legend().itemsFormatter(() => {
        return [
          {text: "Volume", iconFill:"#63B7F7"}
        ]
      });

      this.translate.get('PAGES.Pawnshop.Feed.Charts.OrgChart').subscribe(phrase => {
        this.orgChart.chart.title(phrase);
      });
      this.orgChart.chart.container('org-stats-chart-container');
      document.getElementById('org-stats-chart-container') && this.orgChart.chart.draw();

      this.checkNoDataChart();
    });
  }

  checkNoDataChart() {
    let label = this.orgChart.chart.label().enabled(!this.orgChartData.length);
    this.translate.get('MESSAGE.NoData').subscribe(phrase => {
      label.text(phrase);
    });
    label.fontSize(16);
    label.position("center");
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
