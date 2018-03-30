import {ChangeDetectorRef, Component, OnInit, ViewChild} from '@angular/core';
import {APIService, MessageBoxService, UserService} from "../../services";
import {Page} from "../../models/page";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";
import * as countries from '../../../assets/data/countries.json';

@Component({
  selector: 'app-countries-page',
  templateUrl: './countries-page.component.html',
  styleUrls: ['./countries-page.component.sass']
})
export class CountriesPageComponent implements OnInit {

  public locale: string;
  public loading: boolean;
  public page = new Page();

  public rows:  Array<any> = [];
  public sorts: Array<any> = [{prop: 'date', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'Loading...'};

  public countriesForSelect = [];
  public changeCountryCode: string;

  public form: FormGroup;

  @ViewChild('formDir') formDir;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private _messageBox: MessageBoxService,
    private formBuilder: FormBuilder
  ) {

    this.page.pageNumber = 0;
    this.page.size = 5;
  }

  ngOnInit() {
    this.form = this.formBuilder.group({
      'country': ['', Validators.required],
      'comment': [''],
    });

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.NoData;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.setPage({offset: 0});
    this.loadCountiesFoSelect();
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getBannedCountries(this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .subscribe((data) => {
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

  loadCountiesFoSelect() {
    this.countriesForSelect = countries['sort']((item1, item2) =>
      item1.countryName < item2.countryName ? -1 : (item1.countryName > item2.countryName ? 1 : 0)
    );

    this.form.patchValue({
      country: this.countriesForSelect[0].countryShortCode
    });
  }

  banCountry() {
    this.loading = true;
    this.form.disable();
    this.cdRef.detectChanges();

    const comment = this.form.controls.comment.value;
    this.apiService.banCountry(this.changeCountryCode, comment).subscribe(() => {
      this.setPage({ offset: 0 });

      this.formDir.submitted = false;
      this.form.reset();
      this.form.patchValue({
        country: this.countriesForSelect[0].countryShortCode
      });
      this.form.enable();
    }, () => {
      this._messageBox.alert('Something went wrong, country has not been banned! Sorry :(').subscribe(() => {
        this.formDir.submitted = false;
        this.form.enable();
        this.loading = false;
        this.cdRef.detectChanges();
      });
    });
  }

  unbanCountry(code) {
    this.loading = true;
    this.cdRef.detectChanges();

    this.apiService.unbanCountry(code).subscribe(this.setPage.bind(this, { offset: 0 }), () => {
      this._messageBox.alert('Something went wrong, country has not been unbanned! Sorry :(').subscribe(() =>
      {
        this.loading = false;
        this.cdRef.detectChanges();
      });
    });
  }

}
