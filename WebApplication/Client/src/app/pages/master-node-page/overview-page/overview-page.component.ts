import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component, HostBinding, HostListener,
  OnDestroy,
  OnInit, TemplateRef,
  ViewEncapsulation
} from '@angular/core';
import {APIService, UserService} from "../../../services";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../models/page";
import {BsModalRef, BsModalService} from "ngx-bootstrap";
import {TranslateService} from "@ngx-translate/core";
import 'anychart';
import {OverviewStats} from "../../../interfaces/overview-stats";
import {CurrentActiveNodeList} from "../../../interfaces/current-active-node-list";

@Component({
  selector: 'app-overview-page',
  templateUrl: './overview-page.component.html',
  styleUrls: ['./overview-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None
})
export class OverviewPageComponent implements OnInit, OnDestroy {
  @HostBinding('class') class = 'page';
  @HostListener('window:resize', ['$event'])
  onResize(event) {
    let isMobile = event.target.innerWidth <= 992;
    this.isMobile !== isMobile && this.redrawingMiniCharts();
    this.isMobile = isMobile;
    this.cdRef.markForCheck();
  }

  public overviewStats: OverviewStats;
  public page = new Page();
  public rows: CurrentActiveNodeList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = -1;

  private charts = {};
  private miniCharts = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private paginationHistory: string[] = [];

  modalRef: BsModalRef;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private modalService: BsModalService,
    private translate: TranslateService,
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 992);
    this.setPage(null, true);

    this.apiService.getCurrentActiveNodesStats().subscribe((data: OverviewStats) => {
      this.overviewStats = data;
      this.isDataLoaded = true;
      this.cdRef.markForCheck();
    });

    this.userService.currentLocale.takeUntil(this.destroy$).subscribe(() => {
      if (this.isDataLoaded) {
        this.translate.get('PAGES.MasterNode.Overview.Chart').subscribe(phrase => {
          this.charts['chart'] && this.charts['chart'].title(phrase);
        });
      }
    });
  }

  setPage(from: string, isNext: boolean = true) {
    this.loading = true;

    this.apiService.getCurrentActiveNodesList(from ? from : null)
      .finally(() => {
        this.loading = false;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        if (data.res.list && data.res.list.length) {
          this.rows = data.res.list
        }

        this.rows = this.rows.map((item, i) => {
          item.chartData = [];

          item.history.forEach(node => {
            const date = new Date(node.time * 1000);
            let month = (date.getMonth()+1).toString(),
              day = date.getDate().toString();

            month.length === 1 && (month = '0' + month);
            day.length === 1 && (day = '0' + day);

            const dateString = date.getFullYear() + '-' + month + '-' + day;
            item.chartData.push([dateString, node.gold]);
          });

          setTimeout(() => {
            this.createMiniChart(item.chartData, i);
          }, 0);
          return item;
        });

        if (data.res.list && data.res.list.length) {
          if (!isNext) {
            this.offset--;
            this.paginationHistory.pop();
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = this.rows[this.rows.length - 1].address);
          } else {
            this.offset++;
            this.paginationHistory.push(this.rows[this.rows.length - 1].address);
          }
        }

        if (!data.res.list || (data.res.list && !data.res.list.length)) {
          this.isLastPage = true;
        }
      });
  }

  createMiniChart(data: any[], i: number) {
    anychart.onDocumentReady( () => {
      this.miniCharts[i] = {};
      const container = 'chart-container-' + i;
      const child = document.querySelector(`#${container} > div`);
      child && child.remove();

      this.miniCharts[i]['table'] = anychart.data.table();
      this.miniCharts[i]['table'].addData(data);

      this.miniCharts[i]['mapping'] = this.miniCharts[i]['table'].mapAs();
      this.miniCharts[i]['mapping'].addField('value', 1);

      this.miniCharts[i]['chart'] = anychart.stock();
      this.miniCharts[i]['chart'].scroller().enabled(false);
      this.miniCharts[i]['chart'].crosshair(false);
      this.miniCharts[i]['chart'].plot(0).line(this.miniCharts[i]['mapping']);

      this.miniCharts[i]['chart'].plot(0).xAxis().enabled(false);
      this.miniCharts[i]['chart'].plot(0).yAxis().enabled(false);
      this.miniCharts[i]['chart'].plot(0).legend().enabled(false);

      if (document.getElementById(container)) {
        this.miniCharts[i]['chart'].container(container);
        this.miniCharts[i]['chart'].draw();
      }
    });
  }

  showDetailsChart(data: any[], template: TemplateRef<any>) {
    this.modalRef = this.modalService.show(template);
    document.querySelector('modal-container').classList.add('modal-chart');
    this.initDetailsChart(data);
  }

  initDetailsChart(data: any[]) {
    anychart.onDocumentReady( () => {
      this.charts['table'] && this.charts['table'].remove();

      this.charts['table'] = anychart.data['table']();
      this.charts['table'].addData(data);

      this.charts['mapping'] = this.charts['table'].mapAs();
      this.charts['mapping'].addField('value', 1);

      this.charts['chart'] = anychart.stock();

      this.charts['chart'].plot(0).line(this.charts['mapping']);
      this.charts['chart'].plot(0).legend().itemsFormatter(() => {
        return [
          {text: "Total reward", iconFill:"#63B7F7"}
        ]
      });

      this.translate.get('PAGES.MasterNode.Overview.Chart').subscribe(phrase => {
        this.charts['chart'].title(phrase);
      });

      this.charts['chart'].container('details-chart-container');
      this.charts['chart'].draw();
    });
  }

  redrawingMiniCharts() {
    this.miniCharts = [];
    this.rows.forEach((item, i) => {
      setTimeout(() => {
        this.createMiniChart(item.chartData, i);
      }, 0);
      this.cdRef.markForCheck();
    });
  }

  prevPage() {
    this.setPage(this.paginationHistory[this.paginationHistory.length - 3], false);
  }

  nextPage() {
    this.setPage(this.paginationHistory[this.paginationHistory.length - 1], true);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
