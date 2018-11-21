import { NgModule } from '@angular/core';
import { BrowserModule, Title } from '@angular/platform-browser';
import { HttpClient, HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { JwtModule } from '@auth0/angular-jwt';
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
import { AuthGuard } from './guards';
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
import { BuyPageComponent } from './pages/buy-page/buy-page.component';
import { HistoryPageComponent } from './pages/history-page/history-page.component';
import { HomePageComponent } from './pages/home-page/home-page.component';
import { LimitsPageComponent } from './pages/limits-page/limits-page.component';
import { NotFoundPageComponent } from './pages/not-found-page/not-found-page.component';
import { SellPageComponent } from './pages/sell-page/sell-page.component';
import { SupportPageComponent } from './pages/support-page/support-page.component';
import { TransferPageComponent } from './pages/transfer-page/transfer-page.component';
import { PagerBlockComponent } from './blocks/pager-block/pager-block.component';
import { LoginPageComponent } from './pages/login-page/login-page.component';
import { LoginOntokenPageComponent } from './pages/login-page/login-ontoken-page/login-ontoken-page.component';
import { PasswordResetPageComponent } from './pages/login-page/password-reset-page/password-reset-page.component';
import { RegisterPageComponent } from './pages/register-page/register-page.component';
import { RegisterTfaPageComponent } from './pages/register-page/register-tfa-page/register-tfa-page.component';
import { RegisterSuccessPageComponent } from './pages/register-page/register-success-page/register-success-page.component';
import { RegisterEmailTakenPageComponent } from './pages/register-page/register-email-taken-page/register-email-taken-page.component';
import { RegisterEmailConfirmedPageComponent } from './pages/register-page/register-email-confirmed-page/register-email-confirmed-page.component';
import { SettingsPageComponent } from './pages/settings-page/settings-page.component';
import { SettingsProfilePageComponent } from './pages/settings-page/settings-profile-page/settings-profile-page.component';
import { SettingsVerificationPageComponent } from './pages/settings-page/settings-verification-page/settings-verification-page.component';
import { SettingsTFAPageComponent } from './pages/settings-page/settings-tfa-page/settings-tfa-page.component';
import { SettingsCardsPageComponent } from './pages/settings-page/settings-cards-page/settings-cards-page.component';
import { SettingsSocialPageComponent } from './pages/settings-page/settings-social-page/settings-social-page.component';
import { SettingsActivityPageComponent } from './pages/settings-page/settings-activity-page/settings-activity-page.component';
import { TransparencyPageComponent } from './pages/transparency-page/transparency-page.component';
import { FinancePageComponent } from './pages/finance-page/finance-page.component';
import { NoautocompleteDirective } from './directives/noautocomplete.directive';
import { StaticPagesComponent } from './pages/static-pages/static-pages.component';
import {SafePipe} from "./directives/safe.pipe";
import { LoginDpaRequiredComponent } from './pages/login-page/login-dpa-required/login-dpa-required.component';
import { LoginDpaSignedComponent } from './pages/login-page/login-dpa-signed/login-dpa-signed.component';
import { LegalSecurityPageComponent } from './pages/legal-security-page/legal-security-page.component';
import { SettingsFeesPageComponent } from './pages/settings-page/settings-fees-page/settings-fees-page.component';
import { BuyCryptocurrencyPageComponent } from './pages/buy-page/buy-cryptocurrency-page/buy-cryptocurrency-page.component';
import { SellCryptocurrencyPageComponent } from './pages/sell-page/sell-cryptocurrency-page/sell-cryptocurrency-page.component';
import { MasterNodePageComponent } from './pages/master-node-page/master-node-page.component';
import { LaunchNodePageComponent } from './pages/master-node-page/launch-node-page/launch-node-page.component';
import { TokenMigrationPageComponent } from './pages/master-node-page/token-migration-page/token-migration-page.component';
import { OverviewPageComponent } from './pages/master-node-page/overview-page/overview-page.component';
import { BuyCardPageComponent } from './pages/buy-page/buy-card-page/buy-card-page.component';
import { SellCardPageComponent } from './pages/sell-page/sell-card-page/sell-card-page.component';
import { PaymentCardBlockComponent } from './blocks/payment-card-block/payment-card-block.component';
import { CryptocurrencyBlockComponent } from './blocks/cryptocurrency-block/cryptocurrency-block.component';
import { TimerComponent } from './common/timer/timer.component';
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
import {DeviceDetectorModule} from "ngx-device-detector";
import {NgxMaskModule} from "ngx-mask";
import { WalletPageComponent } from './pages/wallet-page/wallet-page.component';
import { AuthModalComponent } from './common/message-box/auth-modal/auth-modal.component';
import {PawnshopPageComponent} from "./pages/pawnshop-page/pawnshop-page.component";
import { PawnshopFeedPageComponent } from './pages/pawnshop-page/pawnshop-feed-page/pawnshop-feed-page.component';
import { PawnshopBuyPageComponent } from './pages/pawnshop-page/pawnshop-buy-page/pawnshop-buy-page.component';
import { PawnshopSellPageComponent } from './pages/pawnshop-page/pawnshop-sell-page/pawnshop-sell-page.component';
import { AllTicketFeedPageComponent } from './pages/pawnshop-page/pawnshop-feed-page/all-ticket-feed-page/all-ticket-feed-page.component';
import { OrganizationsPageComponent } from './pages/pawnshop-page/pawnshop-feed-page/organizations-page/organizations-page.component';
import { OrganizationsTableComponent } from './pages/pawnshop-page/pawnshop-feed-page/organizations-page/organizations-table/organizations-table.component';
import { PawnshopsTableComponent } from './pages/pawnshop-page/pawnshop-feed-page/organizations-page/pawnshops-table/pawnshops-table.component';
import { FeedTableComponent } from './pages/pawnshop-page/pawnshop-feed-page/organizations-page/feed-table/feed-table.component';
import {CommonService} from "./services/common.service";
import {AccountReductionPipe} from "./pipes/account-reduction";

export function createTranslateLoader(http: HttpClient) {
    return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export function getGoldmintToken() {
	return localStorage.getItem('gmint_token');
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
    DeviceDetectorModule.forRoot(),
    PopoverModule.forRoot(),
    JwtModule.forRoot({
      config: {
        tokenGetter: getGoldmintToken,
        whitelistedDomains: [ environment.apiUrl ],
      }
    }),
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
    BuyPageComponent,
	  MasterNodePageComponent,
    LaunchNodePageComponent,
	  OverviewPageComponent,
	  TokenMigrationPageComponent,
    HistoryPageComponent,
    HomePageComponent,
    LimitsPageComponent,
    NotFoundPageComponent,
    SellPageComponent,
    SupportPageComponent,
    TransferPageComponent,
    PagerBlockComponent,
    LoginPageComponent,
    LoginOntokenPageComponent,
    PasswordResetPageComponent,
    RegisterPageComponent,
    RegisterTfaPageComponent,
    RegisterSuccessPageComponent,
    RegisterEmailTakenPageComponent,
    RegisterEmailConfirmedPageComponent,
    SettingsPageComponent,
    SettingsProfilePageComponent,
    SettingsVerificationPageComponent,
    SettingsTFAPageComponent,
    SettingsCardsPageComponent,
    SettingsSocialPageComponent,
    SettingsActivityPageComponent,
    BlurDirective,
    EqualValidatorDirective,
    TransparencyPageComponent,
    FinancePageComponent,
    NoautocompleteDirective,
    StaticPagesComponent,
    SafePipe,
	  SubstrPipe,
    NoexpPipe,
    GoldDiscount,
    LoginDpaRequiredComponent,
    LoginDpaSignedComponent,
    LegalSecurityPageComponent,
    SettingsFeesPageComponent,
    BuyCryptocurrencyPageComponent,
    SellCryptocurrencyPageComponent,
    BuyCardPageComponent,
    SellCardPageComponent,
    PaymentCardBlockComponent,
    CryptocurrencyBlockComponent,
    TimerComponent,
    LatestRewardPageComponent,
    ScanerPageComponent,
    TxInfoPageComponent,
    TransactionsInBlockPageComponent,
    AllTransactionsPageComponent,
    AllBlocksPageComponent,
    AddressInfoPageComponent,
    WalletPageComponent,
    AuthModalComponent,
    PawnshopPageComponent,
    PawnshopFeedPageComponent,
    PawnshopBuyPageComponent,
    PawnshopSellPageComponent,
    AllTicketFeedPageComponent,
    OrganizationsPageComponent,
    OrganizationsTableComponent,
    PawnshopsTableComponent,
    FeedTableComponent,
    PawnshopDetailsPageComponent,
    AccountReductionPipe
  ],
  exports: [],
  providers: [
    Title,
    AuthGuard,
    MessageBoxService,
    APIService,
    UserService,
    EthereumService,
    GoldrateService,
    CommonService,
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
    MessageBoxComponent,
    AuthModalComponent
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
