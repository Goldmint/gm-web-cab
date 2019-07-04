import {AfterViewInit, ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';

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

  constructor(
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
  }

  ngAfterViewInit() {
    this.initChart();
  }

  initChart() {
    anychart.onDocumentReady( () => {
      this.charts.table = anychart.data.table();
      let data = [
        ["2019-07-01", 0.02, 0.03, 0.04, 0.02],
        ["2019-07-02", 0.03, 0.04, 0.02, 0.06],
        ["2019-07-03", 0.02, 0.03, 0.04, 0.05],
        ["2019-07-04", 0.05, 0.01, 0.03, 0.025]
      ];

      this.charts.table.addData(data);

      this.charts.exchange_gold = this.charts.table.mapAs();
      this.charts.exchange_gold.addField('value', 1);

      this.charts.exchange_mnt = this.charts.table.mapAs();
      this.charts.exchange_mnt.addField('value', 2);

      this.charts.roi_gold = this.charts.table.mapAs();
      this.charts.roi_gold.addField('value', 3);

      this.charts.roi_mnt = this.charts.table.mapAs();
      this.charts.roi_mnt.addField('value', 4);

      this.charts.chart = anychart.stock();

      this.charts.chart.plot(0).line(this.charts.exchange_gold).name('GOLD exchange');
      this.charts.chart.plot(0).line(this.charts.exchange_mnt).name('MNT exchange');
      this.charts.chart.plot(0).line(this.charts.roi_gold).name('ROI GOLD');
      this.charts.chart.plot(0).line(this.charts.roi_mnt).name('ROI MNT');

      this.charts.chart.title('ROI');
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
