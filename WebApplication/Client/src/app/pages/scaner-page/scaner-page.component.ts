import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  HostBinding, OnDestroy,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {APIService, UserService} from "../../services";
import {MessageBoxService} from "../../services/message-box.service";
import 'anychart';
import {TransactionsList} from "../../interfaces/transactions-list";
import {BlocksList} from "../../interfaces/blocks-list";
import * as bs58 from 'bs58';
import * as CRC32 from 'crc-32';
import {combineLatest} from "rxjs/observable/combineLatest";
import {Router} from "@angular/router";
import {TranslateService} from "@ngx-translate/core";
import {Subject} from "rxjs/Subject";
import * as moment from 'moment'
import "moment/locale/ru"
import {CommonService} from "../../services/common.service";
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-scaner-page',
  templateUrl: './scaner-page.component.html',
  styleUrls: ['./scaner-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None
})
export class ScanerPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isValidSumusAddress: boolean = false;
  public isValidDigest: boolean = false;
  public searchAddress: string = '';
  public searchDigest: string = '';
  public numberBlocks: number = 0;
  public numberNodes: number = 0;
  public numberTx: number = 0;
  public anyChartGoldRewardData = [];
  public anyChartMntRewardData = [];
  public anyChartTxData = [];
  public transactionsList: TransactionsList[] = [];
  public prevTransactionsList: any[] = [];
  public blocksList: BlocksList[] = [];
  public prevBlocksList: BlocksList[] = [];
  public feeSwitchModel: {
    type: 'gold'|'mnt'
  };
  public isProduction = environment.isProduction;
  public locale: string = null;
  public balance = {
    gold: 0,
    mnt: 0
  };

  private destroy$: Subject<boolean> = new Subject<boolean>();
  private interval: any;
  private charts: any = {
    goldReward: {},
    mntReward: {},
    tx: {}
  };

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private messageBox: MessageBoxService,
    private translate: TranslateService,
    private commonService: CommonService,
    private router: Router
  ) { }

  ngOnInit() {
    this.feeSwitchModel = { type: 'gold' };

    this.userService.currentLocale.takeUntil(this.destroy$).subscribe((locale) => {
      if (locale) {
        this.locale = locale;
        moment.locale(locale);
      }
      this.cdRef.markForCheck();
    });

    const combined = combineLatest(
      this.apiService.getScannerStatus(),
      this.apiService.getScannerDailyStatistic(),
      this.apiService.getScannerBlockList(null),
      this.apiService.getScannerTxList(null, null, null)
    );

    combined.subscribe((data: any) => {
      this.setStatisticData(data[0]);
      this.setChartsData(data[1].res);
      this.setBlockAndTransactionsInfo(data[2].res.list, data[3].res.list)

      this.initGoldRewardChart();
      this.initMntRewardChart();
      this.initTxChart();

      this.isDataLoaded = true;
      this.cdRef.markForCheck();
    });

    this.interval = setInterval(() => {
      this.updateData(false);
    }, 60000);

    this.apiService.transferCurrentNetwork.takeUntil(this.destroy$).subscribe(() => {
      this.loading = true;
      this.updateData(true);
    });
  }

  updateData(clearOldValues: boolean = false) {
    const combined = combineLatest(
      this.apiService.getScannerStatus(),
      this.apiService.getScannerDailyStatistic(),
      this.apiService.getScannerBlockList(null),
      this.apiService.getScannerTxList(null, null, null)
    );

    combined.subscribe((data: any) => {
      if (clearOldValues) {
        this.anyChartGoldRewardData = [];
        this.anyChartMntRewardData = [];
        this.anyChartTxData = [];
      }

      this.setStatisticData(data[0]);
      this.setChartsData(data[1].res);
      this.setBlockAndTransactionsInfo(data[2].res.list, data[3].res.list);
      this.updateChartsData();

      this.loading = false;
      this.cdRef.markForCheck();
    });
  }

  updateChartsData() {
    this.charts.goldReward.table.remove();
    this.charts.goldReward.table.addData(this.anyChartGoldRewardData);
    this.charts.mntReward.table.remove();
    this.charts.mntReward.table.addData(this.anyChartMntRewardData);
    this.charts.tx.table.remove();
    this.charts.tx.table.addData(this.anyChartTxData);
  }

  setChartsData(res) {
    if (res) {
      res.forEach(item => {
        const date = new Date(item.timestamp * 1000);
        let month = (date.getMonth()+1).toString(),
            day = date.getDate().toString();

        month.length === 1 && (month = '0' + month);
        day.length === 1 && (day = '0' + day);

        const dateString = date.getFullYear() + '-' + month + '-' + day;
        this.anyChartGoldRewardData.push([dateString, +item.fee_gold]);
        this.anyChartMntRewardData.push([dateString, +item.fee_mnt]);
        this.anyChartTxData.push([dateString, item.transactions, +item.volume_gold, +item.volume_mnt]);
      });

      this.anyChartGoldRewardData.splice(0, 1);
      this.anyChartMntRewardData.splice(0, 1);
    }
  }

  setStatisticData(data) {
    this.numberBlocks = data.res.blockchain_state.block_count;
    this.numberNodes = data.res.blockchain_state.node_count;
    this.numberTx = data.res.blockchain_state.transaction_count;
    this.balance.gold = data.res.blockchain_state.balance.gold;
    this.balance.mnt = data.res.blockchain_state.balance.mnt;
  }

  setBlockAndTransactionsInfo(blockList: BlocksList[], txList: TransactionsList[]) {
    if (blockList) {
      this.blocksList = blockList.slice(0, 5);
      this.prevBlocksList = this.commonService.highlightNewItem(this.blocksList, this.prevBlocksList, 'scanner-block-item', 'id');
    }
    if (txList) {
      this.transactionsList = txList.slice(0, 5);
      let txListForHighlight = [];
      this.transactionsList.forEach(item => {
        txListForHighlight.push(item.transaction);
      });
      this.prevTransactionsList = this.commonService.highlightNewItem(txListForHighlight, this.prevTransactionsList, 'scanner-tx-item', 'digest');
    }
  }

  checkAddress(address: string, type: string) {
    this.checkSumusAddressValidity(address, type);
  }

  checkSumusAddressValidity(address: string, type: string) {
    let bytes;
    try {
      bytes = bs58.decode(address);
    } catch (e) {
      type === 'address' ? this.isValidSumusAddress = false : this.isValidDigest = false;
      return
    }

    let field = type === 'address' ? this.searchAddress : this.searchDigest;
    if (bytes.length <= 4 || field.length <= 4) {
      type === 'address' ? this.isValidSumusAddress = false : this.isValidDigest = false;
      return
    }

    let payloadCrc = CRC32.buf(bytes.slice(0, -4));
    let crcBytes = bytes.slice(-4);
    let crc = crcBytes[0] | crcBytes[1] << 8 | crcBytes[2] << 16 | crcBytes[3] << 24;

    type === 'address' ? this.isValidSumusAddress = payloadCrc === crc : this.isValidDigest = payloadCrc === crc;
  }

  initGoldRewardChart() {
    anychart.onDocumentReady( () => {
      this.charts.goldReward.table = anychart.data.table();
      this.charts.goldReward.table.addData(this.anyChartGoldRewardData);

      this.charts.goldReward.mapping_gold = this.charts.goldReward.table.mapAs();
      this.charts.goldReward.mapping_gold.addField('value', 1);

      this.charts.goldReward.chart = anychart.stock();

      this.charts.goldReward.chart.plot(0).line(this.charts.goldReward.mapping_gold).name('GOLD');

      this.charts.goldReward.chart.plot(0).legend().title().useHtml(true);
      this.charts.goldReward.chart.plot(0).legend().titleFormat('');
      this.charts.goldReward.chart.plot(0).legend().itemsFormatter(() => []);

      this.charts.goldReward.chart.title('Collected Fee');
      this.charts.goldReward.chart.container('gold-reward-chart-container');
      this.charts.goldReward.chart.draw();
    });
  }

  initMntRewardChart() {
    anychart.onDocumentReady( () => {
      this.charts.mntReward.table = anychart.data.table();
      this.charts.mntReward.table.addData(this.anyChartMntRewardData);

      this.charts.mntReward.mapping_mnt = this.charts.mntReward.table.mapAs();
      this.charts.mntReward.mapping_mnt.addField('value', 1);

      this.charts.mntReward.chart = anychart.stock();

      this.charts.mntReward.chart.plot(0).line(this.charts.mntReward.mapping_mnt).name('MNT');
      this.charts.mntReward.chart.plot(0).legend().title().useHtml(true);
      this.charts.mntReward.chart.plot(0).legend().titleFormat('');
      this.charts.mntReward.chart.plot(0).legend().itemsFormatter(() => []);

      this.charts.mntReward.chart.title('Collected Fee');
      this.charts.mntReward.chart.container('mnt-reward-chart-container');
      this.charts.mntReward.chart.draw();
    });
  }

  initTxChart() {
    anychart.onDocumentReady( () => {
      this.charts.tx.table = anychart.data.table();
      this.charts.tx.table.addData(this.anyChartTxData);

      this.charts.tx.transactions = this.charts.tx.table.mapAs();
      this.charts.tx.transactions.addField('value', 1);

      this.charts.tx.gold = this.charts.tx.table.mapAs();
      this.charts.tx.gold.addField('value', 2);

      this.charts.tx.mnt = this.charts.tx.table.mapAs();
      this.charts.tx.mnt.addField('value', 3);

      this.charts.tx.chart = anychart.stock();

      this.charts.tx.chart.plot(0).legend().title().useHtml(true);
      this.charts.tx.chart.plot(0).legend().titleFormat('');

      this.charts.tx.chart.plot(0).line(this.charts.tx.transactions).name('Transactions');
      this.charts.tx.chart.plot(0).line(this.charts.tx.gold).name('GOLD');
      this.charts.tx.chart.plot(0).line(this.charts.tx.mnt).name('MNT');

      const logScale = anychart.scales.log();
      this.charts.tx.chart.plot(0).yScale(logScale);

      this.charts.tx.chart.title('Transactions Volume');
      this.charts.tx.chart.container('tx-chart-container');
      this.charts.tx.chart.draw();
    });
  }

  searchByAddress() {
    this.router.navigate(['/scanner/address', this.searchAddress]);
  }

  searchByDigest() {
    this.router.navigate(['/scanner/tx', this.searchDigest]);
  }

  ngOnDestroy() {
    clearInterval(this.interval);
    this.destroy$.next(true);
  }
}
