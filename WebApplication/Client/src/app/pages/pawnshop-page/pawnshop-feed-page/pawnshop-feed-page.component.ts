import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit, ViewEncapsulation} from '@angular/core';
import {CommonService} from "../../../services/common.service";
import {Subject} from "rxjs/Subject";
import {NavigationEnd, Router} from "@angular/router";

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
    private commonService: CommonService,
    private cdRef: ChangeDetectorRef,
    private router: Router
  ) {
    this.router.events.takeUntil(this.destroy$).subscribe((event) => {
      if (event instanceof NavigationEnd) {
        if (event.urlAfterRedirects === '/pawnshop-loans/feed') {
          this.switchModel = { type: 'feed' };
          this.router.navigate(['/pawnshop-loans/feed/all-ticket-feed']);
          return
        }
        this.switchModel = {
          type: event.url.indexOf('all-ticket-feed') >= 0 ? 'feed' : 'organizations'
        }
        this.cdRef.markForCheck();
      }
    });
  }

  ngOnInit() {
    this.commonService.changeFeedTab.takeUntil(this.destroy$).subscribe(() => {
      this.switchModel.type = 'organizations';
      this.cdRef.markForCheck();
    });
  }

  chooseTab() {
    let isFeed = this.switchModel.type === 'feed';
    isFeed ? this.router.navigate(['/pawnshop-loans/feed/all-ticket-feed']) :
             this.router.navigate(['/pawnshop-loans/feed/organizations']);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
