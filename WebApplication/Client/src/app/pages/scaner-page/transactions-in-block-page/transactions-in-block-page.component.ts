import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {APIService, UserService} from "../../../services";
import {Subject} from "rxjs/Subject";
import {ActivatedRoute} from "@angular/router";
import {Page} from "../../../models/page";
import {Block} from "../../../interfaces/block";
import {TransactionsList} from "../../../interfaces/transactions-list";

@Component({
  selector: 'app-transactions-in-block-page',
  templateUrl: './transactions-in-block-page.component.html',
  styleUrls: ['./transactions-in-block-page.component.sass']
})
export class TransactionsInBlockPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public page = new Page();
  public rows: TransactionsList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isMobile: boolean = false;
  public blockNumber: number;
  public block: Block;
  public isLastPage: boolean = false;
  public offset: number = 0;
  public pagination = {
    prev: null,
    next: null
  }

  private paginationHistory: string[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.route.params.takeUntil(this.destroy$).subscribe(params => {
      this.blockNumber = params.id;

      this.apiService.getTransactionsInBlock(this.blockNumber).subscribe((data: any) => {
        this.block = data.res;
        this.isDataLoaded = true;
      });
      this.setPage(null, true);
    });

    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(width => {
      this.isMobile = width <= 992;
      this.cdRef.markForCheck();
    });
  }

  setPage(from: string, isNext: boolean = true) {
    this.loading = true;

    this.apiService.getScannerTxList(this.blockNumber, null, from ? from : null)
      .finally(() => {
        this.loading = false;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        this.rows = data.res.list ? data.res.list : [];

        if (this.rows.length) {
          if (!isNext) {
            this.paginationHistory.pop();
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = this.rows[this.rows.length - 1].transaction.digest);
          }
          isNext && this.paginationHistory.push(this.rows[this.rows.length - 1].transaction.digest);
        } else {
          isNext && this.paginationHistory.push(null);
        }

        (!this.rows.length || (this.offset === 0 && this.rows.length < 10)) && (this.isLastPage = true);
      });
  }

  prevPage() {
    this.offset--;
    this.setPage(this.paginationHistory[this.paginationHistory.length - 3], false);
  }

  nextPage() {
    this.offset++;
    this.setPage(this.paginationHistory[this.paginationHistory.length - 1], true);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
