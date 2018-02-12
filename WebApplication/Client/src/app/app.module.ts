import { NgModule } from '@angular/core';
import { Router } from '@angular/router';
import { BrowserModule, Title } from '@angular/platform-browser';
import { HttpClient, HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { JwtModule } from '@auth0/angular-jwt';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import { /*RECAPTCHA_LANGUAGE,*/ RECAPTCHA_SETTINGS,
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
// import { TranslatePoHttpLoader } from '@biesbjerg/ngx-translate-po-http-loader';
import { registerLocaleData } from '@angular/common';
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
import { BsDropdownModule, ModalModule, ButtonsModule, TabsModule } from 'ngx-bootstrap';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import { NgxQRCodeModule } from '@techiediaries/ngx-qrcode';
// import { NgxPhoneMaskModule } from 'ngx-phone-mask';
// import { InternationalPhoneNumberModule } from 'ngx-international-phone-number';

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
// import { LoginOntokenPageComponent } from './pages/login-page/login-ontoken-page/login-ontoken-page.component';
import { PasswordResetPageComponent } from './pages/login-page/password-reset-page/password-reset-page.component';
import { RegisterPageComponent } from './pages/register-page/register-page.component';
import { RegisterTfaPageComponent } from './pages/register-page/register-tfa-page/register-tfa-page.component';
import { RegisterSuccessPageComponent } from './pages/register-page/register-success-page/register-success-page.component';
import { RegisterEmailTakenPageComponent } from './pages/register-page/register-email-taken-page/register-email-taken-page.component';
import { RegisterEmailConfirmedPageComponent } from './pages/register-page/register-email-confirmed-page/register-email-confirmed-page.component';
import { TfaNotActivatedPageComponent } from './pages/register-page/register-tfa-page/tfa-not-activated-page/tfa-not-activated-page.component';
import { TfaEnablePageComponent } from './pages/register-page/register-tfa-page/tfa-enable-page/tfa-enable-page.component';
import { SettingsPageComponent } from './pages/settings-page/settings-page.component';
import { SettingsProfilePageComponent } from './pages/settings-page/settings-profile-page/settings-profile-page.component';
import { SettingsVerificationPageComponent } from './pages/settings-page/settings-verification-page/settings-verification-page.component';
import { SettingsTFAPageComponent } from './pages/settings-page/settings-tfa-page/settings-tfa-page.component';
import { SettingsCardsPageComponent } from './pages/settings-page/settings-cards-page/settings-cards-page.component';
import { SettingsSocialPageComponent } from './pages/settings-page/settings-social-page/settings-social-page.component';
import { SettingsActivityPageComponent } from './pages/settings-page/settings-activity-page/settings-activity-page.component';
import { TransparencyPageComponent } from './pages/transparency-page/transparency-page.component';
import { DepositPageComponent } from './pages/deposit-page/deposit-page.component';
import { WithdrawPageComponent } from './pages/withdraw-page/withdraw-page.component';
import { FinancePageComponent } from './pages/finance-page/finance-page.component';
import { NoautocompleteDirective } from './directives/noautocomplete.directive';


export function createTranslateLoader(http: HttpClient) {
    return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}
// export function createTranslateLoader(http: HttpClient) {
//   return new TranslatePoHttpLoader(http, 'assets/i18n', '.po');
// }

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
    NgxDatatableModule,
    NgxQRCodeModule,
    // NgxPhoneMaskModule,
    // InternationalPhoneNumberModule,
    HttpClientModule,
    JwtModule.forRoot({
      config: {
        tokenGetter: getGoldmintToken,
        whitelistedDomains: ['localhost:4200', 'gm-cabinet.pashog.net', 'gm-cabinet-dev.pashog.net', 'app.goldmint.io']
      }
    }),
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
    NavbarBlockComponent,
    FooterBlockComponent,
    MessageBoxComponent,
    SpriteComponent,

    BuyPageComponent,
    HistoryPageComponent,
    HomePageComponent,
    LimitsPageComponent,
    NotFoundPageComponent,
    SellPageComponent,
    SupportPageComponent,
    TransferPageComponent,
    PagerBlockComponent,
    LoginPageComponent,
    // LoginOntokenPageComponent,
    PasswordResetPageComponent,
    RegisterPageComponent,
    RegisterTfaPageComponent,
    RegisterSuccessPageComponent,
    RegisterEmailTakenPageComponent,
    RegisterEmailConfirmedPageComponent,
    TfaNotActivatedPageComponent,
    TfaEnablePageComponent,
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
    DepositPageComponent,
    WithdrawPageComponent,
    FinancePageComponent,
    NoautocompleteDirective,
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
    {
      provide: RECAPTCHA_SETTINGS,
      useValue: {
        siteKey: environment.recaptchaSiteKey
      }
    },
    // {
    //   provide: RECAPTCHA_LANGUAGE,
    //   useValue: 'fr'
    // },
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
