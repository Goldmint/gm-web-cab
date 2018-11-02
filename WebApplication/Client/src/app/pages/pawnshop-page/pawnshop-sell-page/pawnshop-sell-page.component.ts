import {Component, HostBinding, OnInit} from '@angular/core';

@Component({
  selector: 'app-pawnshop-sell-page',
  templateUrl: './pawnshop-sell-page.component.html',
  styleUrls: ['./pawnshop-sell-page.component.sass']
})
export class PawnshopSellPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  constructor() { }

  ngOnInit() {
  }

}
