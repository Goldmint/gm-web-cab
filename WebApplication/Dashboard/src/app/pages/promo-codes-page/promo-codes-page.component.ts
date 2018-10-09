import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  ViewChild,
  ViewEncapsulation
} from '@angular/core';
import {APIService, MessageBoxService, UserService} from "../../services";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {Page} from "../../models/page";
import {FormBuilder, FormGroup, Validators} from "@angular/forms";



enum Tables { Deposit, Withdraw }

@Component({
  selector: 'app-promo-codes-page',
  templateUrl: './promo-codes-page.component.html',
  styleUrls: ['./promo-codes-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PromoCodesPageComponent implements OnInit {

  @ViewChild('amount') amount;
  @ViewChild('comment') comment;
  @ViewChild('refuseComment') refuseComment;

  public locale: string;
  public loading: boolean;
  public progress: boolean;
  public isDataLoaded: boolean = false;
  public page = new Page();
  public filterValue: string;
  public promoCodesWriteAccess: boolean = false;
  public filterUsed = {
    used: false,
    unused: false
  };
  private currentUsedFilter: boolean | null = null;

  public rows = [];
  public sorts: Array<any> = [{prop: 'id', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No Data'};
  public form: FormGroup;  

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private formBuilder: FormBuilder,
    private _messageBox: MessageBoxService
  ) {
    this.page.pageNumber = 0;
    this.page.size = 10;
  }

  ngOnInit() {
    this.form = this.formBuilder.group({
      'discount': ['', [Validators.required, Validators.max(100), Validators.pattern(/^(0*[1-9][0-9]*(\.[0-9]+)?|0+\.[0-9]*[1-9][0-9]*)$/)]],
      'limit': ['', [Validators.required, Validators.pattern(/^(0*[1-9][0-9]*(\.[0-9]+)?|0+\.[0-9]*[1-9][0-9]*)$/)]],
      'count': ['', [Validators.required, Validators.pattern(/^[1-9][0-9]*$/)]],
      'valid': ['', [Validators.required, Validators.pattern(/^[1-9][0-9]*$/)]],
      'usageType': [false, null]
    });

    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.messages.emptyMessage = event.translations.NoData;
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.apiService.getProfile().subscribe(profile => {
      let id = +profile['data'].id.slice(1);

      this.apiService.getUsersAccountInfo(id).subscribe(data => {
        data['data'].accessRights.forEach(item => {
          if (item.n === 'PromoCodesWriteAccess') {
            this.promoCodesWriteAccess = item.c;
          }
        });

        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      });
    });

    this.setPage({ offset: 0 });
  }

  onSort(event) {
    this.sorts = event.sorts;
    this.setPage({ offset: 0 });
  }

  promoCodeFilter() {
    this.setPage({ offset: 0 });
  }

  onFilterUsed(status) {
    if (status && this.filterUsed.used) {
      this.currentUsedFilter = true;
      this.filterUsed.unused = false;
    } else if (!status && this.filterUsed.unused) {
      this.currentUsedFilter = this.filterUsed.used = false;
    } else {
      this.currentUsedFilter = null;
    }
    this.setPage({ offset: 0 });
    this.cdRef.markForCheck();
  }

  genPromoCode() {
    this.loading = true;
    this.form.disable();

    const discount = this.form.controls.discount.value;
    const limit = this.form.controls.limit.value;
    const count = this.form.controls.count.value;
    const valid = this.form.controls.valid.value;
    const usageType = this.form.controls.usageType.value ? 2 : 1;

    this.apiService.generatePromoCode("GOLD", limit, discount, usageType, count, valid).subscribe(() => {
      this._messageBox.alert('Success');
      this.form.enable();
      this.form.reset();
      this.loading = false;
      this.setPage({ offset: 0 });
      this.cdRef.markForCheck();
    }, () => {
      this._messageBox.alert('Error');
      this.form.enable();
      this.cdRef.markForCheck();
    });
  }
  
  
   setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;

    this.apiService.getPromoCodesList(this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .subscribe(
        data => {
          this.rows = data.data.items.map(item => {
            item.timeCreated = new Date(item.timeCreated.toString() + 'Z');
            item.timeExpires = new Date(item.timeExpires.toString() + 'Z');
            return item;
          });

          this.page.totalElements = data.data.total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

          this.loading = false;
          this.cdRef.markForCheck();
        });
  }
}
