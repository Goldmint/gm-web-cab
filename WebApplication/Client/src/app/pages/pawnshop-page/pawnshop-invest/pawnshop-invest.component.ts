import {Component, HostBinding, OnInit} from '@angular/core';

@Component({
  selector: 'app-pawnshop-invest',
  templateUrl: './pawnshop-invest.component.html',
  styleUrls: ['./pawnshop-invest.component.sass']
})
export class PawnshopInvestComponent implements OnInit {

  @HostBinding('class') class = 'page';

  constructor() { }

  ngOnInit() {
  }

}
