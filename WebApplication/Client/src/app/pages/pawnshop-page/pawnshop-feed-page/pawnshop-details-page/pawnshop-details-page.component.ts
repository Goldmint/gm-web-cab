import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  HostBinding,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {APIService, UserService} from "../../../../services";
import {ActivatedRoute} from "@angular/router";
import {PawnshopDetails} from "../../../../interfaces/pawnshop-details";
import 'anychart';
import {TranslateService} from "@ngx-translate/core";
import {Subject} from "rxjs/Subject";

@Component({
  selector: 'app-pawnshop-details-page',
  templateUrl: './pawnshop-details-page.component.html',
  styleUrls: ['./pawnshop-details-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None
})
export class PawnshopDetailsPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  public isDataLoaded: boolean = false;
  public pawnshopId: number;
  public pawnshopDetails: PawnshopDetails;
  public rate: number;
  public rateChartData = [];

  private rateChart: any = {};
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private route: ActivatedRoute,
    private translate: TranslateService,
    private userService: UserService
  ) { }

  ngOnInit() {
    this.route.params.takeUntil(this.destroy$).subscribe(params => {
      this.pawnshopId = params.id;
      this.getPawnshopDetails(this.pawnshopId);
    });

    this.userService.currentLocale.takeUntil(this.destroy$).subscribe(() => {
      if (this.isDataLoaded) {
        this.translate.get('PAGES.Pawnshop.Feed.PawnshopDetails.Charts.Rate').subscribe(phrase => {
          this.rateChart.chart.title(phrase);
        });
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
    anychart.onDocumentReady( () => {
      this.rateChart.table = anychart.data['table']();
      this.rateChart.table.addData(this.rateChartData);

      this.rateChart.mapping = this.rateChart['table'].mapAs();
      this.rateChart.mapping.addField('value', 1);

      this.rateChart.chart = anychart.stock();
      this.rateChart.chart.plot(0).line(this.rateChart.mapping).name('Rate');
      this.rateChart.chart.plot(0).legend().itemsFormatter(() => {
        return [
          {text: "Rate", iconFill:"#63B7F7"}
        ]
      });

      this.translate.get('PAGES.Pawnshop.Feed.PawnshopDetails.Charts.Rate').subscribe(phrase => {
        this.rateChart.chart.title(phrase);
      });
      this.rateChart.chart.container('daily-stats-chart-container');
      this.rateChart.chart.draw();
    });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
