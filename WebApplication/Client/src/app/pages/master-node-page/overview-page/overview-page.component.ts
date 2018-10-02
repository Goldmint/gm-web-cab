import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit, TemplateRef,
  ViewEncapsulation
} from '@angular/core';
import {combineLatest} from "rxjs/observable/combineLatest";
import {APIService, UserService} from "../../../services";
//import {Balance} from "../../../interfaces/balance";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../models/page";
import {BsModalRef, BsModalService} from "ngx-bootstrap";
import {TranslateService} from "@ngx-translate/core";

@Component({
  selector: 'app-overview-page',
  templateUrl: './overview-page.component.html',
  styleUrls: ['./overview-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None
})
export class OverviewPageComponent implements OnInit, OnDestroy {

  public numberNodes: number;
  public numberMNT: number;
  public numberReward: number;
  public page = new Page();
  public rows: Array<any> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  //public balance: Balance;

  private charts = {};
  private miniCharts = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();

  modalRef: BsModalRef;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private modalService: BsModalService,
    private translate: TranslateService,
  ) { }

  ngOnInit() {
    this.page.pageNumber = 0;
    this.page.size = 10;

    this.setPage({ offset: 0 });

    /*const combined = combineLatest(
      this.apiService.getNodesCount(),
      this.apiService.getMNTCount(),
      this.apiService.getMNTRewardDay(1),
      this.apiService.getTotalGoldReward()
    );

    combined.subscribe(data => {
      this.numberNodes = data[0]['data'];
      this.numberMNT = data[1]['data'];
      this.numberReward = data[3]['data'];

      this.isDataLoaded = true;
      this.cdRef.markForCheck();
    });*/

    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe((flag: boolean) => {
      this.isMobile !== flag && this.redrawingMiniCharts();
      this.isMobile = flag;
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

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.page.pageNumber = pageInfo.offset;

    /*this.apiService.getActiveNodes(this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .finally(() => {
        this.loading = false;
        this.cdRef.markForCheck();
      })
      .subscribe(
        res => {
          this.rows = res['data'].items.map((item, i) => {
            item.chartData = [];
            item.nodeInfo.launchDate = new Date(item.nodeInfo.launchDate.toString() + 'Z');
            const nodeRewardDict = item.nodeRewardDict[item.nodeInfo.nodeWallet];

            if (nodeRewardDict.length) {
              item.rewardData = {
                ctRewardTotal: nodeRewardDict[nodeRewardDict.length - 1].ctRewardTotal,
                utRewardTotal: nodeRewardDict[nodeRewardDict.length - 1].utRewardTotal
              };

              nodeRewardDict.forEach(node => {
                const date = new Date(node.createDate.toString() + 'Z');
                let month = (date.getMonth()+1).toString();
                month.length === 1 && (month = '0' + month);
                const dateString = date.getFullYear() + '-' + month + '-' + date.getDate();
                item.chartData.push([dateString, node.ctRewardTotal]);
              });
            } else {
              item.rewardData = {
                ctRewardTotal: '-',
                utRewardTotal: '-'
              };
            }

            setTimeout(() => {
              this.createMiniChart(item.chartData, i);
            }, 0);
            return item;
          });

          this.page.totalElements = res['data'].total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);
        });*/
  }

  /*createMiniChart(data: any[], i: number) {
    anychart.onDocumentReady( () => {
      this.miniCharts[i] = {};
      const container = 'chart-container-' + i;
      const child = document.querySelector(`#${container} > div`);
      child && child.remove();
      // if (!this.miniCharts[i]) {
      //   this.miniCharts[i] = {};
      // } else {
      //   this.miniCharts[i]['table'].remove();
      //   this.miniCharts[i]['table'].addData(data);
      //   return;
      // }

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

      // const container = 'chart-container-' + i;
      if (document.getElementById(container)) {
        this.miniCharts[i]['chart'].container(container);
        this.miniCharts[i]['chart'].draw();
      }
    });
  }*/

  showDetailsChart(data: any[], template: TemplateRef<any>) {
    this.modalRef = this.modalService.show(template);
    document.querySelector('modal-container').classList.add('modal-chart');
    //this.initDetailsChart(data);
  }

  /*initDetailsChart(data: any[]) {
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
  }*/

  redrawingMiniCharts() {
    /*this.miniCharts = [];
    this.rows.forEach((item, i) => {
      setTimeout(() => {
        this.createMiniChart(item.chartData, i);
      }, 0);
      this.cdRef.markForCheck();
    });*/
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
