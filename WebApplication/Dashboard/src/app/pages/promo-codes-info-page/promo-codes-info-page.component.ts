import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component, OnDestroy,
  OnInit,
  ViewEncapsulation
} from '@angular/core';
import {APIService, UserService} from "../../services";
import {FormGroup} from "@angular/forms";
import {LangChangeEvent, TranslateService} from "@ngx-translate/core";
import {Page} from "../../models/page";
import {ActivatedRoute} from "@angular/router";
import {Subscription} from "rxjs/Subscription";

@Component({
  selector: 'app-promo-codes-info-page',
  templateUrl: './promo-codes-info-page.component.html',
  styleUrls: ['./promo-codes-info-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PromoCodesInfoPageComponent implements OnInit, OnDestroy {


  public locale: string;
  public loading: boolean;
  public progress: boolean;
  public isDataLoaded: boolean = false;
  public page = new Page();
  public promoCodeId: number;

  public rows = [];
  public sorts: Array<any> = [{prop: 'id', dir: 'desc'}];
  public messages:    any  = {emptyMessage: 'No Data'};
  public form: FormGroup;

  private sub1: Subscription;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private route: ActivatedRoute
  ) {
    this.page.pageNumber = 0;
    this.page.size = 10;
  }

  ngOnInit() {
    this.sub1 = this.route.params.subscribe(params => {
      this.promoCodeId = params.id;
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

  setPage(pageInfo) {
    this.loading = true;
    this.cdRef.detectChanges();
    this.page.pageNumber = pageInfo.offset;
    this.apiService.infoPromoCodes(this.promoCodeId, this.page.pageNumber * this.page.size, this.page.size, this.sorts[0].prop, this.sorts[0].dir)
      .subscribe(
        data => {
          this.rows = data.data.items.map(item => {
            item.timeUsed = new Date(item.timeUsed.toString() + 'Z');
            return item;
          });

          this.page.totalElements = data.data.total;
          this.page.totalPages = Math.ceil(this.page.totalElements / this.page.size);

          this.loading = false;
          this.cdRef.markForCheck();
        });
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }
}
