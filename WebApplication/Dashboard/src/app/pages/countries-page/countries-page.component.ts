import {ChangeDetectorRef, Component, OnInit, ViewChild} from '@angular/core';
import {APIService, MessageBoxService, UserService} from "../../services";
import {Page} from "../../models/page";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";

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
  public sorts: Array<any> = [{prop: 'name', dir: 'asc'}];
  public messages:    any  = {emptyMessage: 'Loading...'};

  private countriesByCode = {};
  private countriesList = [];
  private blockedCountries = [];
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
    this.page.totalPages = 1;
  }

  ngOnInit() {
    this.form = this.formBuilder.group({
      'country': ['', Validators.required],
      'comment': [''],
    });

    this.apiService.getCountriesLocalList().subscribe((localList: any[]) => {
      this.countriesList = localList.sort((item1, item2) =>
        item1.countryName < item2.countryName ? -1 : (item1.countryName > item2.countryName ? 1 : 0)
      );
      localList.forEach(item => this.countriesByCode[item.countryShortCode] = item.countryName);
    });

    this.loadBannedCountries();

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.NoData;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.sortCountries();
  }

  loadCountiesFoSelect() {
    this.countriesForSelect = this.countriesList.filter(item => this.blockedCountries.indexOf(item.countryShortCode) < 0);

    this.form.patchValue({
      country: this.countriesForSelect[0].countryShortCode
    });
  }

  loadBannedCountries() {
    this.loading = true;

    this.apiService.getCountriesBlacklist().subscribe((list) => {
      this.blockedCountries = [];
      this.rows = list.data.map(item => {
        item = item.toUpperCase();

        this.blockedCountries.push(item);

        return {
            code: item,
            name: this.countriesByCode[item],
        }
      });

      this.loadCountiesFoSelect();

      this.page.totalElements = this.page.size = list.data.length;
      this.loading = false;
      this.sortCountries();
    });
  }

  sortCountries() {
    this.rows = this.rows.sort((item1, item2) => {
      item1 = item1[this.sorts[0].prop];
      item2 = item2[this.sorts[0].prop];

      return (item1 < item2 ? -1 : (item1 > item2 ? 1 : 0)) * (this.sorts[0].dir === 'asc' ? 1 : -1);
    });

    this.cdRef.detectChanges();
  }

  banCountry() {
    this.form.disable();

    const comment = this.form.controls.comment.value;
    this.apiService.banCountry(this.changeCountryCode, comment).subscribe(() => {
      this.loadBannedCountries();

      this.formDir.submitted = false;
      this.form.reset();
      this.form.enable();
    }, () => {
      this._messageBox.alert('Something went wrong, country has not been banned! Sorry :(').subscribe(() => {
        this.formDir.submitted = false;
        this.form.enable();
      });
    });
  }

  unbanCountry(code) {
    this.apiService.unbanCountry(code).subscribe(this.loadBannedCountries.bind(this), () => {
      this._messageBox.alert('Something went wrong, country has not been unbanned! Sorry :(').subscribe();
    });
  }

}
