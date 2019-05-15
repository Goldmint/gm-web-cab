import {Component, HostBinding, OnInit} from '@angular/core';

@Component({
  selector: 'app-buy-mntp-page',
  templateUrl: './buy-mntp-page.component.html',
  styleUrls: ['./buy-mntp-page.component.sass']
})
export class BuyMntpPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  constructor() { }

  ngOnInit() {
    const scriptUrls = [
      'https://files.coinmarketcap.com/static/widget/currency.js',
      'https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js'
    ];

    const scripts: any = document.head.querySelectorAll('script');
    scripts && scripts.forEach(script => {
      scriptUrls.indexOf(script.src) >= 0 && script.parentNode.removeChild(script);
    });

    let script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = scriptUrls[0];
    document.head.appendChild(script);
  }

}
