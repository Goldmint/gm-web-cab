import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {APIService, UserService} from "../../../services";
import {Subject} from "rxjs/Subject";
import {ActivatedRoute} from "@angular/router";
import {Page} from "../../../models/page";
import {Balance} from "../../../interfaces/balance";

@Component({
  selector: 'app-transactions-in-block-page',
  templateUrl: './transactions-in-block-page.component.html',
  styleUrls: ['./transactions-in-block-page.component.sass']
})
export class TransactionsInBlockPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public page = new Page();
  public rows: Array<any> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public balance: Balance;
  public blockNumber: number;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.page.pageNumber = 0;
    this.page.size = 10;

    this.route.params.takeUntil(this.destroy$).subscribe(params => {
      this.blockNumber = params.id;
      this.setPage({ offset: 0 });
    });

    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(width => {
      this.isMobile = width <= 992;
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

    this.apiService.getTransactionsInBlock(this.blockNumber, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
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
