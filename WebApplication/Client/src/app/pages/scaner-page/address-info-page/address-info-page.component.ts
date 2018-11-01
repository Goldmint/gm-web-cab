import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  HostBinding,
  OnDestroy,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {MessageBoxService} from "../../../services/message-box.service";
import {APIService, UserService} from "../../../services";
import {Subject} from "rxjs/Subject";
import {ActivatedRoute} from "@angular/router";
import {Page} from "../../../models/page";
import {WalletInfo} from "../../../interfaces/wallet-info";
import {TransactionsList} from "../../../interfaces/transactions-list";

@Component({
  selector: 'app-address-info-page',
  templateUrl: './address-info-page.component.html',
  styleUrls: ['./address-info-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None
})
export class AddressInfoPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public page = new Page();
  public rows: TransactionsList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public walletInfo: WalletInfo;
  public isLastPage: boolean = false;
  public offset: number = 0;
  public pagination = {
    prev: '0',
    next: '0'
  }

  private sumusAddress: string;
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private messageBox: MessageBoxService,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.page.pageNumber = 0;
    this.page.size = 10;

    this.route.params.takeUntil(this.destroy$).subscribe(params => {
      this.sumusAddress = params.id;
      this.setPage(0);

      this.apiService.getWalletBalance(this.sumusAddress).subscribe((data: any) => {
        this.walletInfo = data.res;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      }, () => {
        this.cdRef.markForCheck();
      })
    });

    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(width => {
      this.isMobile = width <= 992;
      this.cdRef.markForCheck();
    });
  }

  setPage(from: number | string) {
    this.loading = true;

    this.apiService.getScannerTxList(0, this.sumusAddress, from)
      .finally(() => {
        this.loading = false;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        this.rows = data.res.list ? data.res.list : [];

        this.pagination.prev = this.offset > 1 ? this.pagination.next : '0';
        this.pagination.next = this.rows.length && this.rows[this.rows.length - 1].transaction.digest;

        !this.rows.length && (this.isLastPage = true);
      });
  }

  prevPage() {
    this.offset--;
    this.setPage(this.pagination.prev);
  }

  nextPage() {
    this.offset++;
    this.setPage(this.pagination.next);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
