import { NgModule } from '@angular/core';
import { BrowserModule, Title } from '@angular/platform-browser';
import { HttpClient, HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RECAPTCHA_SETTINGS,
  RecaptchaModule
} from 'ng-recaptcha';

/*
  Application main imports
 */
import { environment } from '../environments/environment';
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
import { NavbarBlockComponent } from './blocks/navbar-block/navbar-block.component';
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
import { MasterNodePageComponent } from './pages/master-node-page/master-node-page.component';
import { LaunchNodePageComponent } from './pages/master-node-page/launch-node-page/launch-node-page.component';
import { OverviewPageComponent } from './pages/master-node-page/overview-page/overview-page.component';
import {GoldDiscount} from "./pipes/gold-discount";
import {SubstrPipe} from "./pipes/substr.pipe";
import {NoexpPipe} from "./pipes/noexp.pipe";
import {LatestRewardPageComponent} from "./pages/master-node-page/overview-page/latest-reward-page/latest-reward-page.component";
import {ScanerPageComponent} from "./pages/scaner-page/scaner-page.component";
import {TxInfoPageComponent} from "./pages/scaner-page/tx-info-page/tx-info-page.component";
import {TransactionsInBlockPageComponent} from "./pages/scaner-page/transactions-in-block-page/transactions-in-block-page.component";
import {AllTransactionsPageComponent} from "./pages/scaner-page/all-transactions-page/all-transactions-page.component";
import {AllBlocksPageComponent} from "./pages/scaner-page/all-blocks-page/all-blocks-page.component";
import {AddressInfoPageComponent} from "./pages/scaner-page/address-info-page/address-info-page.component";
import {MomentModule} from "ngx-moment";
import {NgxMaskModule} from "ngx-mask";
import {PawnshopPageComponent} from "./pages/pawnshop-page/pawnshop-page.component";
import { PawnshopFeedPageComponent } from './pages/pawnshop-page/pawnshop-feed-page/pawnshop-feed-page.component';
import { AllTicketFeedPageComponent } from './pages/pawnshop-page/pawnshop-feed-page/all-ticket-feed-page/all-ticket-feed-page.component';
import { OrganizationsTableComponent } from './pages/pawnshop-page/pawnshop-feed-page/organizations-table/organizations-table.component';
import { PawnshopsTableComponent } from './pages/pawnshop-page/pawnshop-feed-page/pawnshops-table/pawnshops-table.component';
import { FeedTableComponent } from './pages/pawnshop-page/pawnshop-feed-page/feed-table/feed-table.component';
import {CommonService} from "./services/common.service";
import {AccountReductionPipe} from "./pipes/account-reduction";
import { RewardTransactionsPageComponent } from './pages/master-node-page/overview-page/reward-transactions-page/reward-transactions-page.component';
import { BlockchainPoolPageComponent } from './pages/blockchain-pool-page/blockchain-pool-page.component';
import { HoldTokensPageComponent } from './pages/blockchain-pool-page/hold-tokens-page/hold-tokens-page.component';
import {PoolService} from "./services/pool.service";
import { MobileNavbarBlockComponent } from './blocks/mobile-navbar-block/mobile-navbar-block.component';
import { BuyMntpPageComponent } from './pages/buy-mntp-page/buy-mntp-page.component';
import { NetworkSwitcherBlockComponent } from './blocks/network-switcher-block/network-switcher-block.component';
import { SwapMntpComponent } from './pages/swap-mntp/swap-mntp.component';

export function createTranslateLoader(http: HttpClient) {
    return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

@NgModule({
  imports: [
    AppRouting,
    BrowserModule,
    FormsModule,
    ReactiveFormsModule,
    RecaptchaModule.forRoot(),
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
    }),
    NgxMaskModule.forRoot()
  ],
  declarations: [
    AppComponent,
    LanguageSwitcherBlockComponent,
    HeaderBlockComponent,
    NavbarBlockComponent,
    FooterBlockComponent,
    MessageBoxComponent,
    SpriteComponent,
	  MasterNodePageComponent,
    LaunchNodePageComponent,
	  OverviewPageComponent,
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
    LatestRewardPageComponent,
    ScanerPageComponent,
    TxInfoPageComponent,
    TransactionsInBlockPageComponent,
    AllTransactionsPageComponent,
    AllBlocksPageComponent,
    AddressInfoPageComponent,
    PawnshopPageComponent,
    PawnshopFeedPageComponent,
    AllTicketFeedPageComponent,
    OrganizationsTableComponent,
    PawnshopsTableComponent,
    FeedTableComponent,
    AccountReductionPipe,
    RewardTransactionsPageComponent,
    BlockchainPoolPageComponent,
    HoldTokensPageComponent,
    MobileNavbarBlockComponent,
    BuyMntpPageComponent,
    NetworkSwitcherBlockComponent,
    SwapMntpComponent
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
      provide: RECAPTCHA_SETTINGS,
      useValue: {
        siteKey: environment.recaptchaSiteKey
      }
    },
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
