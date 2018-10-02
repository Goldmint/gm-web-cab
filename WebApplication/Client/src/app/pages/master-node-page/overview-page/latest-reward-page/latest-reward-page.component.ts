import {ChangeDetectorRef, Component, HostBinding, OnInit} from '@angular/core';
//import {Balance} from "../../../../interfaces/balance";
import {APIService, UserService} from "../../../../services";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../../models/page";

@Component({
  selector: 'app-latest-reward-page',
  templateUrl: './latest-reward-page.component.html',
  styleUrls: ['./latest-reward-page.component.sass']
})
export class LatestRewardPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  public page = new Page();
  public rows: Array<any> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  //public balance: Balance;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.page.pageNumber = 0;
    this.page.size = 10;

    this.setPage({ offset: 0 });

    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe((flag: boolean) => {
      this.isMobile = flag;
      this.cdRef.markForCheck();
    });
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.page.pageNumber = pageInfo.offset;

    /*this.apiService.getRewardTransactions(this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
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
        });*/
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
