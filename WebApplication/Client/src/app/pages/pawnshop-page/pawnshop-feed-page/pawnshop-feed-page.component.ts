import {Component, HostBinding, OnDestroy, OnInit, ViewEncapsulation} from '@angular/core';
import {UserService} from "../../../services";

@Component({
  selector: 'app-pawnshop-feed-page',
  templateUrl: './pawnshop-feed-page.component.html',
  styleUrls: ['./pawnshop-feed-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class PawnshopFeedPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public switchModel: {
    type: 'feed'|'organizations'
  };

  constructor(
    private userService: UserService
  ) { }

  ngOnInit() {
    this.switchModel = {
      type: 'feed'
    };
  }

  ngOnDestroy() {
    this.userService.organizationStepper$.next(null);
  }
}
