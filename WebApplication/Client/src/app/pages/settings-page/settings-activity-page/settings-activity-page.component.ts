import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';

import { Page } from '../../../models/page';
import { ActivityRecord } from '../../../interfaces';
import { UserService, APIService } from '../../../services';

@Component({
  selector: 'app-settings-activity-page',
  templateUrl: './settings-activity-page.component.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsActivityPageComponent implements OnInit {
  public locale: string;
  public loading: boolean = true;
  public page = new Page();

  public rows:  Array<ActivityRecord> = [];
  public sort: {
    prop: string,
    dir: 'asc'|'desc'
  }

  private _sortState: number = 0;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef) {

    this.page.pageNumber = 0;
    this.page.size = 10;

    this.sort = {prop: 'date', dir: 'desc'};
  }

  ngOnInit() {
    this._userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
      this._cdRef.detectChanges();
    });

    this.setPage({ offset: 0 });
  }

  onSort(prop: string) {
    this._sortState = (this._sortState >= 2) ? 0 : this._sortState + 1;

    switch (this._sortState) {
      case 1:
        this.sort = {prop: prop, dir: 'desc'};
        break;

      case 2:
        this.sort = {prop: prop, dir: 'asc'};
        break;

      default:
        this.sort = {prop: undefined, dir: undefined};
        break;
    }

    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.page.pageNumber = pageInfo.offset;

    console.log('page', this.page);

    this._apiService.getActivity(this.page.pageNumber * this.page.size, this.page.size, this.sort.prop, this.sort.dir)
      .subscribe(
        res => {
          this.rows = res.data.items;

          this.page.totalElements = res.data.total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

          this.loading = false;
          this._cdRef.detectChanges();

          const tableTitle = document.getElementById('pageSectionTitle');
          if (tableTitle && tableTitle.getBoundingClientRect().top < 0) {
            tableTitle.scrollIntoView();
          }
        });
  }

}
