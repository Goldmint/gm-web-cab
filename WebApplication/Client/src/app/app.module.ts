import { NgModule } from '@angular/core';
import { BrowserModule, Title } from '@angular/platform-browser';
import { HttpClient, HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

/*
  Application main imports
 */
import { AppComponent } from './app.component';
import { AppRouting } from './app.routing';

/*
  Guards and Services
 */
import { MessageBoxService, APIService, UserService, EthereumService, GoldrateService } from './services';
import { APIHttpInterceptor } from './common/api/api-http.interceptor'

/*
  Translation and Locale
 */
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import {DatePipe, registerLocaleData} from '@angular/common';
import localeRu from '@angular/common/locales/ru';
registerLocaleData(localeRu);

/*
  Directives
 */
import { BlurDirective } from './directives/blur.directive';
import { EqualValidatorDirective } from './directives/equal-validator.directive';

/*
  UI components
 */
import {
  BsDropdownModule,
  ModalModule,
  ButtonsModule,
  TabsModule,
  TypeaheadModule, PopoverModule
} from 'ngx-bootstrap';

import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import { NgxQRCodeModule } from '@techiediaries/ngx-qrcode';

/*
  Blocks
 */
import { HeaderBlockComponent } from './blocks/header-block/header-block.component';
import { LanguageSwitcherBlockComponent } from './blocks/language-switcher-block/language-switcher-block.component';
import { FooterBlockComponent } from './blocks/footer-block/footer-block.component';
import { MessageBoxComponent }  from './common/message-box/message-box.component';
import { SpriteComponent }      from './common/sprite/sprite.component';

/*
  Pages
 */
import { NotFoundPageComponent } from './pages/not-found-page/not-found-page.component';
import { PagerBlockComponent } from './blocks/pager-block/pager-block.component';
import { NoautocompleteDirective } from './directives/noautocomplete.directive';
import { StaticPagesComponent } from './pages/static-pages/static-pages.component';
import {SafePipe} from "./directives/safe.pipe";
import { LegalSecurityPageComponent } from './pages/legal-security-page/legal-security-page.component';
import {GoldDiscount} from "./pipes/gold-discount";
import {SubstrPipe} from "./pipes/substr.pipe";
import {NoexpPipe} from "./pipes/noexp.pipe";
import {ScanerPageComponent} from "./pages/scaner-page/scaner-page.component";
import {TxInfoPageComponent} from "./pages/scaner-page/tx-info-page/tx-info-page.component";
import {TransactionsInBlockPageComponent} from "./pages/scaner-page/transactions-in-block-page/transactions-in-block-page.component";
import {AllTransactionsPageComponent} from "./pages/scaner-page/all-transactions-page/all-transactions-page.component";
import {AllBlocksPageComponent} from "./pages/scaner-page/all-blocks-page/all-blocks-page.component";
import {AddressInfoPageComponent} from "./pages/scaner-page/address-info-page/address-info-page.component";
import {MomentModule} from "ngx-moment";
import {CommonService} from "./services/common.service";
import {AccountReductionPipe} from "./pipes/account-reduction";
import { BlockchainPoolPageComponent } from './pages/blockchain-pool-page/blockchain-pool-page.component';
import {PoolService} from "./services/pool.service";
import { MobileNavbarBlockComponent } from './blocks/mobile-navbar-block/mobile-navbar-block.component';
import { BuyMntpPageComponent } from './pages/buy-mntp-page/buy-mntp-page.component';
import { SwapMntpComponent } from './pages/swap-mntp/swap-mntp.component';
import { BuySellGoldPageComponent } from './pages/buy-sell-gold-page/buy-sell-gold-page.component';

export function createTranslateLoader(http: HttpClient) {
    return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

@NgModule({
  imports: [
    AppRouting,
    BrowserModule,
    FormsModule,
    ReactiveFormsModule,
    BsDropdownModule.forRoot(),
    ModalModule.forRoot(),
    ButtonsModule.forRoot(),
    TabsModule.forRoot(),
    MomentModule,
    NgxDatatableModule,
    TypeaheadModule,
    NgxQRCodeModule,
    HttpClientModule,
    PopoverModule.forRoot(),
    TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: createTranslateLoader,
        deps: [HttpClient]
      }
    })
  ],
  declarations: [
    AppComponent,
    LanguageSwitcherBlockComponent,
    HeaderBlockComponent,
    FooterBlockComponent,
    MessageBoxComponent,
    SpriteComponent,
    NotFoundPageComponent,
    PagerBlockComponent,
    BlurDirective,
    EqualValidatorDirective,
    NoautocompleteDirective,
    StaticPagesComponent,
    SafePipe,
	  SubstrPipe,
    NoexpPipe,
    GoldDiscount,
    LegalSecurityPageComponent,
    ScanerPageComponent,
    TxInfoPageComponent,
    TransactionsInBlockPageComponent,
    AllTransactionsPageComponent,
    AllBlocksPageComponent,
    AddressInfoPageComponent,
    AccountReductionPipe,
    BlockchainPoolPageComponent,
    MobileNavbarBlockComponent,
    BuyMntpPageComponent,
    SwapMntpComponent,
    BuySellGoldPageComponent
  ],
  exports: [],
  providers: [
    Title,
    MessageBoxService,
    APIService,
    UserService,
    EthereumService,
    GoldrateService,
    CommonService,
    PoolService,
    DatePipe,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: APIHttpInterceptor,
      multi: true
    }
  ],
  entryComponents: [
    MessageBoxComponent
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
