import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';
import {OrganizationList} from "../../../../../interfaces/organization-list";
import {Subject} from "rxjs/Subject";
import {APIService} from "../../../../../services";
import {Page} from "../../../../../models/page";
import {CommonService} from "../../../../../services/common.service";

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
  public offset: number = 0;
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
    private commonService: CommonService
  ) { }

  ngOnInit() {
    this.commonService.setTwoOrganizationStep$.takeUntil(this.destroy$).subscribe((data: any) => {
      if (data !== null) {
        let selected = {
          step: null,
          id: data.id,
          org: data.name,
          pawnshop: null
        }
        this.commonService.organizationStepper$.next(selected);
        }
    });
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
        this.rows = data.res.list ? data.res.list : [];
        this.rows.forEach(item => {
          this.selected[0] && item.id === this.selected[0].id && (this.selected = [item]);
        });

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

  prevPage() {
    this.offset--;
    this.setPage(this.paginationHistory[this.paginationHistory.length - 3], false);
  }

  nextPage() {
    this.offset++;
    this.setPage(this.paginationHistory[this.paginationHistory.length - 1], true);
  }

  onSelect({ selected }: any) {
    let data = {
      step: null,
      id: selected[0].id,
      org: selected[0].name,
      pawnshop: null
    }
    this.commonService.organizationStepper$.next(data);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
