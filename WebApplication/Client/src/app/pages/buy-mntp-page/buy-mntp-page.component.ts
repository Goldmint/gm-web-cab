import {AfterViewInit, ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {APIService} from "../../services";

@Component({
  selector: 'app-buy-mntp-page',
  templateUrl: './buy-mntp-page.component.html',
  styleUrls: ['./buy-mntp-page.component.sass']
})
export class BuyMntpPageComponent implements OnInit, AfterViewInit, OnDestroy {

  @HostBinding('class') class = 'page';

  private charts: any = {};
  private window = window;
  private scriptUrls = [
    'https://files.coinmarketcap.com/static/widget/currency.js',
    'https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js',
    'https://widget-convert.bancor.network/v1'
  ];
  public bancorWidgetLoaded: boolean = false;

  private chartData = [];

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    const coinMarketScript = document.createElement('script');
    coinMarketScript.type = 'text/javascript';
    coinMarketScript.src = this.scriptUrls[0];
    document.head.appendChild(coinMarketScript);

    const bancorScript = document.createElement('script');
    bancorScript.type = 'text/javascript';
    bancorScript.src = this.scriptUrls[2];
    document.head.appendChild(bancorScript);
    bancorScript.onload= () => {
      const widgetInstance = this.window['BancorConvertWidget'].createInstance({
        "type": "1",
        "blockchainTypes": [
          "ethereum"
        ],
        "baseCurrencyId": "5a03590f08849f0001097d29",
        "pairCurrencyId": "5937d635231e97001f744267",
        "primaryColor": "#000000",
        "widgetContainerId": "bancor-wc-id-1",
        "displayCurrency": "USD",
        "primaryColorHover": "#666",
        "hideVolume": true
      });
      this.bancorWidgetLoaded = true;
      this.cdRef.markForCheck();
    };

    this.apiService.getScannerDailyStatistic(true).subscribe((data: any) => {
      if (data && data.res) {
        data.res.forEach(item => {
          const date = new Date(item.timestamp * 1000);
          let month = (date.getMonth()+1).toString(),
            day = date.getDate().toString();

          month.length === 1 && (month = '0' + month);
          day.length === 1 && (day = '0' + day);

          const dateString = date.getFullYear() + '-' + month + '-' + day;
          const usd = (+item.fee_gold * item.coin_price.gold_usd + +item.fee_mnt * item.coin_price.mntp_usd) * 0.75 / +item.total_stake * 10000;
          const btc = (+item.fee_gold * item.coin_price.gold_btc + +item.fee_mnt * item.coin_price.mntp_btc) * 0.75 / +item.total_stake * 10000;

          this.chartData.push([
            dateString,
            isNaN(usd) ? 0 : Math.ceil((usd) * 100) / 100,
            isNaN(btc) ? 0 : Math.ceil((btc) * 100000000) / 100000000,
            +item.total_stake
          ]);
        });
        this.initChart();
      }
    });
  }

  ngAfterViewInit() {

  }

  initChart() {
    anychart.onDocumentReady( () => {
      this.charts.table = anychart.data.table();
      this.charts.table.addData(this.chartData);

      this.charts.usd = this.charts.table.mapAs();
      this.charts.usd.addField('value', 1);

      this.charts.btc = this.charts.table.mapAs();
      this.charts.btc.addField('value', 2);

      this.charts.stake = this.charts.table.mapAs();
      this.charts.stake.addField('value', 3);

      this.charts.chart = anychart.stock();

      this.charts.chart.plot(0).legend().title().useHtml(true);
      this.charts.chart.plot(0).legend().titleFormat('');

      this.charts.chart.plot(0).line(this.charts.usd).name('USD');
      this.charts.chart.plot(0).line(this.charts.btc).name('BTC');
      this.charts.chart.plot(0).line(this.charts.stake).name('Stake').enabled(false);

      const logScale = anychart.scales.log();
      this.charts.chart.plot(0).yScale(logScale);

      this.charts.chart.plot(0).yAxis().orientation('right');
      this.charts.chart.right(70);

      this.charts.chart.title('Reward per 10 000 MNT (GOLD + MNT)');
      this.charts.chart.title().align('left');

      this.charts.chart.container('chart-container');
      this.charts.chart.draw();
    });
  }

  ngOnDestroy() {
    const scripts: any = document.head.querySelectorAll('script');
    scripts && scripts.forEach(script => {
      this.scriptUrls.indexOf(script.src) >= 0 && script.parentNode.removeChild(script);
    });
  }

}
