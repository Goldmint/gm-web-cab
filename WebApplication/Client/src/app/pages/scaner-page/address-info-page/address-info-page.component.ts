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
import {TranslateService} from "@ngx-translate/core";

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
  public offset: number = -1;
  public pagination = {
    prev: null,
    next: null
  };
  public isInvalidAddress: boolean = false;

  private paginationHistory: string[] = [];
  private sumusAddress: string;
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private messageBox: MessageBoxService,
    private translate: TranslateService,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.page.pageNumber = 0;
    this.page.size = 10;

    this.route.params.takeUntil(this.destroy$).subscribe(params => {
      this.sumusAddress = params.id;
      this.setPage(null, true);

      this.apiService.getWalletBalance(this.sumusAddress).subscribe((data: any) => {
        this.walletInfo = data.res;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      }, (error) => {
        this.catchError(error);
      })
    });

    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(width => {
      this.isMobile = width <= 992;
      this.cdRef.markForCheck();
    });
  }

  setPage(from: string, isNext: boolean = true) {
    this.loading = true;

    this.apiService.getScannerTxList(null, this.sumusAddress, from ? from : null)
      .finally(() => {
        this.loading = false;
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
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = this.rows[this.rows.length - 1].transaction.digest);
          } else {
            this.offset++;
            this.paginationHistory.push(this.rows[this.rows.length - 1].transaction.digest);
          }
        }

        if (!data.res.list || (data.res.list && !data.res.list.length) || (this.offset === 0 && this.rows.length < 10)) {
          this.isLastPage = true;
        }
      }, (error) => {
        this.catchError(error);
      });
  }

  catchError(error) {
    if (error.status === 400) {
      this.isInvalidAddress = true;
    } else {
      this.translate.get('APIErrors.wrong').subscribe(phrase => {
        this.messageBox.alert(phrase);
      });
    }
    this.isDataLoaded = true;
    this.cdRef.markForCheck();
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
