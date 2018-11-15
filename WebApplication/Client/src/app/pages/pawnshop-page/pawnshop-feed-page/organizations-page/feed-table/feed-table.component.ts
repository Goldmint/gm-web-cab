import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';
import {APIService, UserService} from "../../../../../services";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../../../models/page";
import {FeedList} from "../../../../../interfaces/feed-list";

@Component({
  selector: 'app-feed-table',
  templateUrl: './feed-table.component.html',
  styleUrls: ['./feed-table.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FeedTableComponent implements OnInit {

  public pawnshopId: number;
  public page = new Page();
  public rows: FeedList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public loading: boolean = false;
  public isMobile: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = 0;
  public pagination = {
    prev: null,
    next: null
  }

  private paginationHistory: number[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(width => {
      this.isMobile = width <= 992;
      this.cdRef.markForCheck();
    });

    this.userService.organizationStepper$.takeUntil(this.destroy$).subscribe((data: {step: number, id: number}) => {
      if (data !== null && data.step === 3) {
        this.pawnshopId = data.id;
        this.setPage(this.pawnshopId, null, true);
      }
    });
  }

  setPage(org: number, from: number = null, isNext: boolean = true) {
    this.loading = true;

    this.apiService.getPawnList(org, from >= 0 ? from : null)
      .finally(() => {
        this.loading = false;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        this.rows = data.res.list ? data.res.list : [];

        if (this.rows.length) {
          if (!isNext) {
            this.paginationHistory.pop();
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = +this.rows[this.rows.length - 1].id);
          }
          isNext && this.paginationHistory.push(+this.rows[this.rows.length - 1].id);
        } else {
          isNext && this.paginationHistory.push(null);
        }

        (!this.rows.length || (this.offset === 0 && this.rows.length < 10)) && (this.isLastPage = true);
      });
  }

  prevPage() {
    this.offset--;
    this.setPage(this.pawnshopId, this.paginationHistory[this.paginationHistory.length - 3], false);
  }

  nextPage() {
    this.offset++;
    this.setPage(this.pawnshopId, this.paginationHistory[this.paginationHistory.length - 1], true);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
