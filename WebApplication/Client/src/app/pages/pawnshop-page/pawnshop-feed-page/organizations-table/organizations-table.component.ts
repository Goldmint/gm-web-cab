import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';
import {OrganizationList} from "../../../../interfaces/organization-list";
import {Subject} from "rxjs/Subject";
import {APIService} from "../../../../services/index";
import {Page} from "../../../../models/page";
import {CommonService} from "../../../../services/common.service";
import {Router} from "@angular/router";

@Component({
  selector: 'app-organizations-table',
  templateUrl: './organizations-table.component.html',
  styleUrls: ['./organizations-table.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrganizationsTableComponent implements OnInit {

  public page = new Page();
  public rows: OrganizationList[] = [];
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
  public selected: OrganizationList[] = [];

  private paginationHistory: number[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private commonService: CommonService,
    private router: Router
  ) { }

  ngOnInit() {
    this.setPage(null, true);
  }

  setPage(from: number, isNext: boolean = true) {
    this.loading = true;

    this.apiService.getOrganizationList(from >= 0 ? from : null)
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

  onSelect({ selected }: any) {
    this.router.navigate(['/pawnshop-loans/feed/pawnshop/', selected[0].id]);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
