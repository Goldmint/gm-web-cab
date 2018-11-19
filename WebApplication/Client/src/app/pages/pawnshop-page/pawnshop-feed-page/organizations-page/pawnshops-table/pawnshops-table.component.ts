import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewEncapsulation} from '@angular/core';
import {APIService} from "../../../../../services";
import {Subject} from "rxjs/Subject";
import {Page} from "../../../../../models/page";
import {PawnshopList} from "../../../../../interfaces/pawnshop-list";
import {CommonService} from "../../../../../services/common.service";
import {OrgStepperData} from "../../../../../models/org-stepper-data";

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
  public pagination = {
    prev: null,
    next: null
  }
  public selected: PawnshopList[] = [];
  public stepperData: OrgStepperData;

  private paginationHistory: number[] = [];
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private commonService: CommonService
  ) { }

  ngOnInit() {
    this.commonService.organizationStepper$.takeUntil(this.destroy$).subscribe((data: OrgStepperData) => {
      if (data !== null && data.step === 2) {
        this.paginationHistory = [];
        this.stepperData = data;
        this.orgId = data.id;
        this.setPage(this.orgId, null, true);
      }
    });
  }

  setPage(org: number, from: number = null, isNext: boolean = true) {
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
    this.setPage(this.orgId, this.paginationHistory[this.paginationHistory.length - 3], false);
  }

  nextPage() {
    this.offset++;
    this.setPage(this.orgId, this.paginationHistory[this.paginationHistory.length - 1], true);
  }

  onSelect({ selected }) {
    let data = this.stepperData;
    data.step = null;
    data.id = selected[0].id;
    data.pawnshop = selected[0].name;
    this.commonService.organizationStepper$.next(data);
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
