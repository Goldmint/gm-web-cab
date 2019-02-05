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
  public offset: number = -1;
  public pagination = {
    prev: null,
    next: null
  }

  private paginationHistory: number[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.setPage(null, true);

    this.isMobile = (window.innerWidth <= 992);
    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(width => {
      this.isMobile = width <= 992;
      this.cdRef.markForCheck();
    });
  }

  setPage(from: number, isNext: boolean = true) {
    this.loading = true;

    this.apiService.getScannerBlockList(from >= 0 ? from : null)
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
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = +this.rows[this.rows.length - 1].id);
          } else {
            this.offset++;
            this.paginationHistory.push(+this.rows[this.rows.length - 1].id);
          }
        }

        if (!data.res.list || (data.res.list && !data.res.list.length) || (this.offset === 0 && this.rows.length < 10)) {
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
