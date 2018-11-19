import {Component, HostBinding, OnDestroy, OnInit, ViewEncapsulation} from '@angular/core';
import {CommonService} from "../../../services/common.service";
import {Subject} from "rxjs/Subject";

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
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private commonService: CommonService
  ) { }

  ngOnInit() {
    this.switchModel = {
      type: 'feed'
    };

    this.commonService.setTwoOrganizationStep$.takeUntil(this.destroy$).subscribe(id => {
      id !== null && (this.switchModel.type = 'organizations');
    })
  }

  ngOnDestroy() {
    this.commonService.organizationStepper$.next(null);
    this.destroy$.next(true);
  }
}
