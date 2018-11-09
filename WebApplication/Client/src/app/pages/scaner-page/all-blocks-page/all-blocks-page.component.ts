import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  HostBinding,
  OnDestroy,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {Subject} from "rxjs/Subject";
import {APIService, UserService} from "../../../services";
import {Page} from "../../../models/page";
import {BlocksList} from "../../../interfaces/blocks-list";

@Component({
  selector: 'app-all-blocks-page',
  templateUrl: './all-blocks-page.component.html',
  styleUrls: ['./all-blocks-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AllBlocksPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public page = new Page();
  public rows: BlocksList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = 0;
  public pagination = {
    prev: null,
    next: null
  }

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.setPage(null);

    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(width => {
      this.isMobile = width <= 992;
      this.cdRef.markForCheck();
    });
  }

  setPage(from: number) {
    this.loading = true;

    this.apiService.getScannerBlockList(from)
      .finally(() => {
        this.loading = false;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        this.rows = data.res.list ? data.res.list : [];

        this.pagination.prev = this.offset > 1 ? this.pagination.next : null;
        this.pagination.next = this.rows.length && +this.rows[this.rows.length - 1].id;

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
