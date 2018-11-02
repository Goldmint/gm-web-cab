import {Component, HostBinding, OnInit} from '@angular/core';

@Component({
  selector: 'app-pawnshop-feed-page',
  templateUrl: './pawnshop-feed-page.component.html',
  styleUrls: ['./pawnshop-feed-page.component.sass']
})
export class PawnshopFeedPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  constructor() { }

  ngOnInit() {
  }

}
