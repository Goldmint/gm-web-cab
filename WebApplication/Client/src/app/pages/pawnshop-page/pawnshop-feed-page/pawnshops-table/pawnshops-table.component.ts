import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';
import {APIService} from "../../../../services/index";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../../models/page";
import {PawnshopList} from "../../../../interfaces/pawnshop-list";
import {CommonService} from "../../../../services/common.service";
import {ActivatedRoute, Router} from "@angular/router";

@Component({
  selector: 'app-pawnshops-table',
  templateUrl: './pawnshops-table.component.html',
  styleUrls: ['./pawnshops-table.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PawnshopsTableComponent implements OnInit {

  public orgId: number;
  public page = new Page();
  public rows: PawnshopList[] = [];
  public messages: any  = {emptyMessage: 'No data'};
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isLastPage: boolean = false;
  public offset: number = 0;
  public selected: PawnshopList[] = [];
  public orgName: string;

  private paginationHistory: number[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private commonService: CommonService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.route.params.takeUntil(this.destroy$).subscribe(params => {
      this.setPage(params.id, null, true, true);
    });
  }

  ngOnInit() { }

  setPage(org: number, from: number = null, isNext: boolean = true, isRouteChange: boolean = false) {
    this.loading = true;

    this.apiService.getPawnshopList(org, from >= 0 ? from : null)
      .finally(() => {
        this.loading = false;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.isLastPage = false;
        this.rows = data.res.list ? data.res.list : [];

        if (isRouteChange) {
          this.rows.length ? this.getOrganizationName(this.rows[0].org_id) : this.orgName = '-';
          this.cdRef.markForCheck();
        }

        if (this.rows.length) {
          if (!isNext) {
            this.paginationHistory.pop();
            this.paginationHistory.length === 1 && (this.paginationHistory[0] = +this.rows[this.rows.length - 1].id);
          }
          isNext && this.paginationHistory.push(+this.rows[this.rows.length - 1].id);
        } else {
          isNext && this.paginationHistory.push(null);
        }

        (!this.rows.length || (this.offset === 0 && this.rows.length < 10)) && (this.isLastPage = true);
      });
  }

  getOrganizationName(orgId: number) {
    this.apiService.getOrganizationsName().subscribe((orgList: any) => {
      let list = orgList.res.list;
      for (let key in list) {
        if (orgId === +key) {
          this.orgName = list[key];
          this.cdRef.markForCheck();
        }
      }
    });
  }

  prevPage() {
    this.offset--;
    this.setPage(this.orgId, this.paginationHistory[this.paginationHistory.length - 3], false, false);
  }

  nextPage() {
    this.offset++;
    this.setPage(this.orgId, this.paginationHistory[this.paginationHistory.length - 1], true, false);
  }

  back() {
    this.router.navigate(['/pawnshop-loans/feed/organizations']);
  }

  onSelect({ selected }) {
    this.router.navigate(['/pawnshop-loans/feed/organization-feed/', selected[0].id]);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
