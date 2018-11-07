import {ChangeDetectionStrategy, ChangeDetectorRef, Component, HostBinding, HostListener, OnInit} from '@angular/core';
import {APIService, UserService} from "../../../../services";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../../models/page";

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
  public rows: Array<any> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 992);
    this.page.pageNumber = 0;
    this.page.size = 10;

    this.setPage({ offset: 0 });
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getRewardTransactions(this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .finally(() => {
        this.loading = false;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      })
      .subscribe(
        res => {
          this.rows = res['data'].items.map(item => {
            item.timeStamp = new Date(item.timeStamp.toString() + 'Z');
            return item;
          });

          this.page.totalElements = res['data'].total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);
        });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
