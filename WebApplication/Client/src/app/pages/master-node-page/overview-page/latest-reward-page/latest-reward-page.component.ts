import {ChangeDetectionStrategy, ChangeDetectorRef, Component, HostBinding, HostListener, OnInit} from '@angular/core';
import {APIService, UserService} from "../../../../services";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../../models/page";
import {LatestReward} from "../../../../interfaces/latest-reward";

@Component({
  selector: 'app-latest-reward-page',
  templateUrl: './latest-reward-page.component.html',
  styleUrls: ['./latest-reward-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LatestRewardPageComponent implements OnInit {

  @HostBinding('class') class = 'page';
  @HostListener('window:resize', ['$event'])
  onResize(event) {
    let isMobile = event.target.innerWidth <= 992;
    this.isMobile !== isMobile && (this.isMobile = isMobile);
    this.cdRef.markForCheck();
  }

  public page = new Page();
  public rows: LatestReward[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = -1;

  private destroy$: Subject<boolean> = new Subject<boolean>();
  private paginationHistory: number[] = [];

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 992);
    this.setPage(null, true);
  }

  setPage(from: number, isNext: boolean = true) {
    this.loading = true;

    this.apiService.getLatestRewardList(from ? from : null)
      .finally(() => {
        this.loading = false;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        if (data.res.list && data.res.list.length) {
          this.rows = data.res.list;
        }

        if (data.res.list && data.res.list.length) {
          if (!isNext) {
            this.offset--;
            this.paginationHistory.pop();
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = this.rows[this.rows.length - 1].id);
          } else {
            this.offset++;
            this.paginationHistory.push(this.rows[this.rows.length - 1].id);
          }
        }

        if (!data.res.list || (data.res.list && !data.res.list.length)) {
          this.isLastPage = true;
        }
      });
  }

  prevPage() {
    this.setPage(this.paginationHistory[this.paginationHistory.length - 3], false);
  }

  nextPage() {
    this.setPage(this.paginationHistory[this.paginationHistory.length - 1], true);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
