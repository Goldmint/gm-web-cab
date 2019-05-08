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
  }

}
