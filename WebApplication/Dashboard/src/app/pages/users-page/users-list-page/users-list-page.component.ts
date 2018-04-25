import {ChangeDetectorRef, Component, OnInit, ViewChild, ViewEncapsulation} from '@angular/core';
import {Page} from "../../../models/page";
import {APIService, MessageBoxService, UserService} from "../../../services";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";

@Component({
  selector: 'app-users-list-page',
  templateUrl: './users-list-page.component.html',
  styleUrls: ['./users-list-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class UsersListPageComponent implements OnInit {

  public locale: string;
  public loading: boolean;
  public page = new Page();

  public rows:  Array<any> = [];
  public sorts: Array<any> = [{prop: 'id', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No Data'};

  public form: FormGroup;

  public currentUser = {};
  public filterValue = '';
  public userIDForApprove: number;
  public isModalShow = false;

  @ViewChild('formDir') formDir;
  @ViewChild('proofResidency') proofResidency;

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
      'filter': ['', Validators.required]
    });

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.NoData;
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

  showProvedResidenceModal(id) {
    this.isModalShow = true;
    this.userIDForApprove = id;

    this.cdRef.markForCheck();
  }

  setProvedResidence() {
    const link = this.proofResidency.value.ticketLink;
    this.apiService.setProvedResidence(this.userIDForApprove, link)
      .finally(() => {
        this.proofResidency.reset();
        this.cdRef.markForCheck();
      })
      .subscribe(() =>{
        this._messageBox.alert('Approved');
        this.isModalShow = false;
    }, () => {
      this._messageBox.alert('Error. Something went wrong');
    });
  }

  setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getUsersList(this.filterValue, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
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

  userFilter() {
    this.filterValue = this.form.controls.filter.value;
    this.setPage({ offset: 0 });
  }
}
