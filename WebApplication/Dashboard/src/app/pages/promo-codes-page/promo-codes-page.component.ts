import {ChangeDetectorRef, Component, OnInit, ViewChild, ViewEncapsulation} from '@angular/core';
import {APIService, MessageBoxService, UserService} from "../../services";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {Page} from "../../models/page";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";



enum Tables { Deposit, Withdraw }

@Component({
  selector: 'app-promo-codes-page',
  templateUrl: './promo-codes-page.component.html',
  styleUrls: ['./promo-codes-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class PromoCodesPageComponent implements OnInit {

  @ViewChild('amount') amount;
  @ViewChild('comment') comment;
  @ViewChild('refuseComment') refuseComment;

  public tables = Tables;
  public currentTable: any;

  public locale: string;
  public loading: boolean;
  public progress: boolean;
  public page = new Page();
  public isExclude = false;
  public showModal: boolean = false;

  public rows = [];
  public sorts: Array<any> = [{prop: 'id', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No Data'};

  public dataType: number = 1;
  public currentId: number;
  public userInfo: object;
  public isRefuse = false;
  public degree18 = Math.pow(10, -18);
  public degree3 = Math.pow(10, -3);
  
  public form: FormGroup;
  private filterValue = '';

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
    //this.currentTable = this.tables.Deposit;
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

  excludeCompleted(exclude) {
    this.isExclude = exclude.checked;
    this.setPage({ offset: this.page.pageNumber });
    this.cdRef.detectChanges();
  }
  
    genPromoCode() {
    this.loading = true;
    this.form.disable();
    this.cdRef.detectChanges();	
	
    this.apiService.generatePromoCode(3, 10000000000000000000, 25000, 1, 200).subscribe(() => {
      this._messageBox.alert('Changes saved');
      this.loading = false;
    }, error => {
      this._messageBox.alert('Error');
    });
  }
  
  
   setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getPromoCodesList("", null, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
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

}
