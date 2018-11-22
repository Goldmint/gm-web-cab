import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';
import {Subject} from "rxjs/Subject";
import {APIService, UserService} from "../../../../services";
import {FeedList} from "../../../../interfaces/feed-list";
import {Page} from "../../../../models/page";
import {CommonService} from "../../../../services/common.service";
import {Observable} from "rxjs/Observable";
import {Router} from "@angular/router";

@Component({
  selector: 'app-all-ticket-feed-page',
  templateUrl: './all-ticket-feed-page.component.html',
  styleUrls: ['./all-ticket-feed-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AllTicketFeedPageComponent implements OnInit {

  public page = new Page();
  public rows: FeedList[] = [];
  public prevRows: FeedList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public loading: boolean = false;
  public isMobile: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = 0;

  private paginationHistory: number[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private interval;

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

    Observable.combineLatest(
      this.apiService.getOrganizationsName(),
      this.apiService.getPawnList(org, from >= 0 ? from : null)
    ).finally(() => {
        this.loading = false;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        let orgList = data[0].res.list;
        this.rows = data[1].res.list ? data[1].res.list : [];

        this.rows.forEach(row => {
          for (let key in orgList) {
            row.org_id === +key && (row.org_name = orgList[key]);
          }
        });

        this.prevRows = this.commonService.highlightNewItem(this.rows, this.prevRows, 'table-row', 'id');

        (!this.rows.length || (this.offset === 0 && this.rows.length < 10)) && (this.isLastPage = true);

        this.interval = setInterval(() => {
          this.setPage(org, from, null);
        }, 20000);

        this.pagination(isNext);
        this.cdRef.markForCheck();
      });
  }

  pagination(isNext) {
    if(isNext === null) return;

    if (this.rows.length) {
      if (!isNext) {
        this.paginationHistory.pop();
        this.paginationHistory.length === 1 && (this.paginationHistory[0] = +this.rows[this.rows.length - 1].id);
      }
      isNext && this.paginationHistory.push(+this.rows[this.rows.length - 1].id);
    } else {
      isNext && this.paginationHistory.push(null);
    }
  }

  selectOrganization(id: number) {
    this.commonService.changeFeedTab.next(true);
    this.router.navigate(['/pawnshop-loans/feed/pawnshop/', id]);
  }

  prevPage() {
    this.offset--;
    this.prevRows = [];
    this.setPage(null, this.paginationHistory[this.paginationHistory.length - 3], false);
  }

  nextPage() {
    this.offset++;
    this.prevRows = [];
    this.setPage(null, this.paginationHistory[this.paginationHistory.length - 1], true);
  }

  ngOnDestroy() {
    clearInterval(this.interval);
    this.destroy$.next(true);
  }

}