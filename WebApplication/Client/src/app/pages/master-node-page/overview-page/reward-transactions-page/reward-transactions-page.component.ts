import {ChangeDetectorRef, Component, HostBinding, HostListener, OnInit} from '@angular/core';
import {APIService, UserService} from "../../../../services";
import {Page} from "../../../../models/page";
import {Subject} from "rxjs/Subject";
import {ActivatedRoute} from "@angular/router";
import {RewardTransactions} from "../../../../interfaces/reward-transactions";

@Component({
  selector: 'app-reward-transactions-page',
  templateUrl: './reward-transactions-page.component.html',
  styleUrls: ['./reward-transactions-page.component.sass']
})
export class RewardTransactionsPageComponent implements OnInit {

  @HostBinding('class') class = 'page';
  @HostListener('window:resize', ['$event'])
  onResize(event) {
    let isMobile = event.target.innerWidth <= 992;
    this.isMobile !== isMobile && (this.isMobile = isMobile);
    this.cdRef.markForCheck();
  }

  public page = new Page();
  public rows: RewardTransactions[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = 0;

  private rewardId: number;
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private paginationHistory: number[] = [];

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private route: ActivatedRoute
  ) {
    this.route.params.takeUntil(this.destroy$).subscribe(params => {
        this.rewardId = params.id;
        this.setPage(null, true);
      });
  }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 992);
  }

  setPage(from: number, isNext: boolean = true) {
    this.loading = true;

    this.apiService.getRewardTransactions(this.rewardId, from ? from : null)
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
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = this.rows[this.rows.length - 1].tx_nonce);
          }
          isNext && this.paginationHistory.push(this.rows[this.rows.length - 1].tx_nonce);
        } else {
          isNext && this.paginationHistory.push(null);
        }

        !this.rows.length  && (this.isLastPage = true);
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
