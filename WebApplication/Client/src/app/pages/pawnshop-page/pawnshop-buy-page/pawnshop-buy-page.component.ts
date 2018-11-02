import {Component, HostBinding, OnInit} from '@angular/core';

@Component({
  selector: 'app-pawnshop-buy-page',
  templateUrl: './pawnshop-buy-page.component.html',
  styleUrls: ['./pawnshop-buy-page.component.sass']
})
export class PawnshopBuyPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  constructor() { }

  ngOnInit() {
  }

}
