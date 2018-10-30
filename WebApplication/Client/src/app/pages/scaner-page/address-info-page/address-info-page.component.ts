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
import {Balance} from "../../../interfaces/balance";
import {WalletInfo} from "../../../interfaces/wallet-info";

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
  public rows: Array<any> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public walletInfo: WalletInfo;

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
      this.setPage({ offset: 0 });
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

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getTxByAddress(this.sumusAddress, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .subscribe(
        res => {
          this.rows = res['data'].items.map(item => {
            item.timeStamp = new Date(item.timeStamp.toString() + 'Z');
            return item;
          });

          this.page.totalElements = res['data'].total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

          this.loading = false;
          this.cdRef.markForCheck();
        }, () => {
          this.loading = false;
          this.cdRef.markForCheck();
        });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
