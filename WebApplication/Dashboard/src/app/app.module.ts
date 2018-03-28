import { NgModule } from '@angular/core';
import { BrowserModule, Title } from '@angular/platform-browser';
import { HttpClient, HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { JwtModule } from '@auth0/angular-jwt';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import { RECAPTCHA_SETTINGS, RecaptchaModule } from 'ng-recaptcha';

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
import { MessageBoxService, APIService, UserService } from './services';
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
import { BsDropdownModule, ModalModule, ButtonsModule, TabsModule } from 'ngx-bootstrap';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import { NgxQRCodeModule } from '@techiediaries/ngx-qrcode';

/*
  Blocks
 */
import { HeaderBlockComponent } from './blocks/header-block/header-block.component';
import { LanguageSwitcherBlockComponent } from './blocks/language-switcher-block/language-switcher-block.component';
import { NavbarBlockComponent } from './blocks/navbar-block/navbar-block.component';
import { MessageBoxComponent }  from './common/message-box/message-box.component';
import { SpriteComponent }      from './common/sprite/sprite.component';

/*
  Pages
 */
import { NotFoundPageComponent } from './pages/not-found-page/not-found-page.component';
import { LoginPageComponent } from './pages/login-page/login-page.component';
import { SettingsPageComponent } from './pages/settings-page/settings-page.component';
import { TransparencyPageComponent } from './pages/transparency-page/transparency-page.component';
import { CountriesPageComponent } from './pages/countries-page/countries-page.component';
import { NoautocompleteDirective } from './directives/noautocomplete.directive';
import {SafePipe} from "./directives/safe.pipe";
import { UsersPageComponent } from './pages/users-page/users-page.component';
import { OplogPageComponent } from './pages/users-page/oplog-page/oplog-page.component';
import { AccessRightsPageComponent } from './pages/users-page/access-rights-page/access-rights-page.component';
import { UserPageComponent } from './pages/users-page/user-page/user-page.component';
import { UsersListPageComponent } from './pages/users-page/users-list-page/users-list-page.component';
import {SettingsProfilePageComponent} from "./pages/settings-page/settings-profile-page/settings-profile-page.component";


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
    NgxDatatableModule,
    NgxQRCodeModule,
    HttpClientModule,
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
    })
  ],
  declarations: [
    AppComponent,
    LanguageSwitcherBlockComponent,
    HeaderBlockComponent,
    NavbarBlockComponent,
    MessageBoxComponent,
    SpriteComponent,
    NotFoundPageComponent,
    LoginPageComponent,
    SettingsPageComponent,
    SettingsProfilePageComponent,
    BlurDirective,
    EqualValidatorDirective,
    TransparencyPageComponent,
    NoautocompleteDirective,
    SafePipe,
    CountriesPageComponent,
    UsersPageComponent,
    OplogPageComponent,
    AccessRightsPageComponent,
    UserPageComponent,
    UsersListPageComponent,
  ],
  exports: [],
  providers: [
    Title,
    AuthGuard,
    MessageBoxService,
    APIService,
    UserService,
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
