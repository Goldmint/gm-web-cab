import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {Subject} from "rxjs/Subject";
import {APIService, UserService} from "../../../../services";
import {FeedList} from "../../../../interfaces/feed-list";
import {Page} from "../../../../models/page";
import {CommonService} from "../../../../services/common.service";
import {Router} from "@angular/router";

@Component({
  selector: 'app-all-ticket-feed-page',
  templateUrl: './all-ticket-feed-page.component.html',
  styleUrls: ['./all-ticket-feed-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AllTicketFeedPageComponent implements OnInit, OnDestroy {

  public page = new Page();
  public rows: FeedList[] = [];
  public prevRows: FeedList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public loading: boolean = false;
  public isMobile: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = -1;
  public currentDate = new Date().getTime();

  private paginationHistory: number[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private interval;
  private isFirstLoad: boolean = true;

  rowClass = (row) => {
    let itemClass = 'table-row-' + row.id;
    return itemClass;
  }

  constructor(
    private apiService: APIService,
    private commonService: CommonService,
    private userService: UserService,
    private router: Router,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(width => {
      this.isMobile = width <= 992;
      this.cdRef.markForCheck();
    });
    this.setPage(null, null, true);
  }

  setPage(org: number, from: number = null, isNext: boolean = true) {
    this.loading = true;
    clearInterval(this.interval);
    this.isFirstLoad = true;

    this.commonService.getPawnShopOrganization.takeUntil(this.destroy$).subscribe(orgList => {
      if (orgList) {
        this.isFirstLoad && this.apiService.getPawnList(org, from >= 0 ? from : null)
          .finally(() => {
            this.loading = false;
            this.isDataLoaded = true;
            this.cdRef.markForCheck();
          }).subscribe((data: any) => {
            this.isLastPage = false;
            if (data.res.list && data.res.list.length) {
              this.rows = data.res.list;
            }

            this.rows.forEach(row => {
              for (let key in orgList) {
                row.org_id === +key && (row.org_name = orgList[key]);
              }
            });

            // this.prevRows = this.commonService.highlightNewItem(this.rows, this.prevRows, 'table-row', 'id');

            if (!data.res.list || (data.res.list && !data.res.list.length)) {
              this.isLastPage = true;
            }

            this.interval = setInterval(() => {
              this.setPage(org, from, null);
            }, 20000);

            this.cdRef.markForCheck();
            if(isNext === null) return;

            if (data.res.list && data.res.list.length) {
              if (!isNext) {
                this.offset--;
                this.prevRows = [];
                this.paginationHistory.pop();
                this.paginationHistory.length === 1 && (this.paginationHistory[0] = +this.rows[this.rows.length - 1].id);
              } else {
                this.offset++;
                this.prevRows = [];
                this.paginationHistory.push(+this.rows[this.rows.length - 1].id);
              }
            }
            this.cdRef.markForCheck();
          });
        this.isFirstLoad = false;
      }
    });
  }

  selectOrganization(id: number) {
    this.commonService.changeFeedTab.next(true);
    this.router.navigate(['/pawnshop-loans/feed/pawnshop/', id]);
  }

  prevPage() {
    this.setPage(null, this.paginationHistory[this.paginationHistory.length - 3], false);
  }

  nextPage() {
    this.setPage(null, this.paginationHistory[this.paginationHistory.length - 1], true);
  }

  ngOnDestroy() {
    clearInterval(this.interval);
    this.destroy$.next(true);
  }

}
