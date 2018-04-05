import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {APIService, MessageBoxService, UserService} from "../../services";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {Page} from "../../models/page";
import {TransparencyRecord} from "../../interfaces";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";

@Component({
  selector: 'app-swift-page',
  templateUrl: './swift-page.component.html',
  styleUrls: ['./swift-page.component.sass']
})
export class SwiftPageComponent implements OnInit {

  public locale: string;
  public loading: boolean;
  public page = new Page();
  public isExclude = false;

  public rows:  Array<TransparencyRecord> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'Loading...'};

  public form: FormGroup;
  private filterValue = '';

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private formBuilder: FormBuilder
  ) {

    this.page.pageNumber = 0;
    this.page.size = 5;
  }

  ngOnInit() {

    this.form = this.formBuilder.group({
      'filter': ['', Validators.required]
    });

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.NoData;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.setPage({ offset: 0 }, this.filterValue);
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 }, this.filterValue);
  }

  excludeCompleted(exclude) {
    this.isExclude = exclude.checked;
    this.setPage({ offset: this.page.pageNumber }, this.filterValue);
    this.cdRef.detectChanges();
  }

  setPage(pageInfo, filter) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getSwiftList(filter, this.isExclude, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir).subscribe(data => {
      this.rows = data.data.items;

      this.page.totalElements = data.data.total;
      this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

      this.loading = false;
      this.cdRef.detectChanges();
    })

  }

  swiftFilter() {
    this.filterValue = this.form.controls.filter.value;
    this.setPage({ offset: 0 }, this.filterValue);
  }

}
