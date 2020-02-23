import {Component, HostBinding, OnInit} from '@angular/core';

@Component({
  selector: 'app-buy-sell-gold-page',
  templateUrl: './buy-sell-gold-page.component.html',
  styleUrls: ['./buy-sell-gold-page.component.sass']
})
export class BuySellGoldPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  constructor() { }

  ngOnInit() { }

}
