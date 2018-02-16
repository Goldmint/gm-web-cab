import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';

import { Page } from '../../models/page';
import { TransparencySummary, TransparencyRecord } from '../../interfaces';
import { APIService, UserService } from '../../services';
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import { BigNumber } from 'bignumber.js';


@Component({
  selector: 'app-transparency-page',
  templateUrl: './transparency-page.component.html',
  styleUrls: ['./transparency-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransparencyPageComponent implements OnInit {
  public locale: string;
  public loading: boolean;
  public page = new Page();

  public summary: TransparencySummary;

  public rows:  Array<TransparencyRecord> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'Loading...'};

  public form: FormGroup;
  private notSelectedFile = true;
  private amount = null;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private formBuilder: FormBuilder) {

    this.page.pageNumber = 0;
    this.page.size = 5;
  }

  ngOnInit() {
    this.form = this.formBuilder.group({
      'amount': ['', Validators.required],
      'comment': ['', Validators.required]
    });

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.PAGES.History.Table.EmptyMessage;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.setPage({ offset: 0 });
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getTransparency(this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .subscribe(
        data => {
          this.rows = data.data;

          this.page.totalElements = data.count;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

          this.loading = false;
          this.cdRef.detectChanges();

          const tableTitle = document.getElementById('pageSectionTitle');
          if (tableTitle && tableTitle.getBoundingClientRect().top < 0) {
            tableTitle.scrollIntoView();
          }
        });
  }

  onAmountChanged(value) {
    this.amount = null;
    if (value != '') {
      this.amount = new BigNumber(value);
      this.amount = this.amount.decimalPlaces(6, BigNumber.ROUND_DOWN);
    }
  }

  onChangeFile(e) {
    e.target.value ? this.notSelectedFile = false : this.notSelectedFile = true;
  }

  upload() {
  }

}
