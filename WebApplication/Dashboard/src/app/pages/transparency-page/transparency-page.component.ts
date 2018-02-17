import {
  Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef,
  ViewChild
} from '@angular/core';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';

import { Page } from '../../models/page';
import { TransparencySummary, TransparencyRecord } from '../../interfaces';
import { APIService, UserService, MessageBoxService } from '../../services';
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
  private amount = null;

  @ViewChild('file') selectedFile;
  @ViewChild('formDir') formDir;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private formBuilder: FormBuilder,
    private _messageBox: MessageBoxService
    ) {

    this.page.pageNumber = 0;
    this.page.size = 5;
  }

  ngOnInit() {
    this.form = this.formBuilder.group({
      'amount': ['', Validators.required],
      'comment': ['', Validators.required],
      'file': ['', Validators.required],
      'realFile': ['']
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
          this.rows = data.data.items;

          this.page.totalElements = data.data.total;
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

  onChangeFile(event) {
    let file = event.target.files[0];
    this.form.controls['file'].setValue(file ? file.name : '');
  }

  upload() {
    let fileBrowser = this.selectedFile.nativeElement;
    if (fileBrowser.files && fileBrowser.files[0]) {
      this.form.disable();

      const formData = new FormData();
      formData.append("arg", fileBrowser.files[0]);

      let errorFn = () => {
        this._messageBox.alert('Something went wrong, transparency has not been added! Sorry :(').subscribe();
        this.formDir.submitted = false;
        this.form.enable();
      };

      this.apiService.addIPFSFile(formData)
        .subscribe(
          data => {
            this.apiService.addTransparency(data['Hash'], this.form.value.amount, this.form.value.comment).subscribe(
              () => {
                this._messageBox.alert('Transparency has been added!').subscribe(() => {
                  this.formDir.submitted = false;
                  this.form.reset();
                  this.form.enable();
                });
                this.setPage({ offset: this.page.pageNumber });
              },
              errorFn
            );
          },
          errorFn
        )
    }
  }

}
