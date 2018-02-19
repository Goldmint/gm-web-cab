import { NgModule } from '@angular/core';
import { RouterModule, Routes, CanActivate } from '@angular/router';

import { AuthGuard } from './guards/index';

import { HomePageComponent }     from './pages/home-page/home-page.component';
import { LimitsPageComponent }   from './pages/limits-page/limits-page.component';
import { NotFoundPageComponent } from './pages/not-found-page/not-found-page.component';
import { PagerBlockComponent }   from './blocks/pager-block/pager-block.component';
import { LoginPageComponent }         from './pages/login-page/login-page.component';
// import { LoginOntokenPageComponent }  from './pages/login-page/login-ontoken-page/login-ontoken-page.component';
import { RegisterPageComponent } from './pages/register-page/register-page.component';
import { RegisterTfaPageComponent } from './pages/register-page/register-tfa-page/register-tfa-page.component';
import { RegisterSuccessPageComponent } from './pages/register-page/register-success-page/register-success-page.component';
import { RegisterEmailTakenPageComponent } from './pages/register-page/register-email-taken-page/register-email-taken-page.component';
import { RegisterEmailConfirmedPageComponent } from './pages/register-page/register-email-confirmed-page/register-email-confirmed-page.component';
import { TfaEnablePageComponent }       from './pages/register-page/register-tfa-page/tfa-enable-page/tfa-enable-page.component';
import { TfaNotActivatedPageComponent } from './pages/register-page/register-tfa-page/tfa-not-activated-page/tfa-not-activated-page.component';
import { SettingsPageComponent }             from './pages/settings-page/settings-page.component';
import { SettingsProfilePageComponent }      from './pages/settings-page/settings-profile-page/settings-profile-page.component';
import { SettingsVerificationPageComponent } from './pages/settings-page/settings-verification-page/settings-verification-page.component';
import { SettingsTFAPageComponent }          from './pages/settings-page/settings-tfa-page/settings-tfa-page.component';
import { SettingsCardsPageComponent }        from './pages/settings-page/settings-cards-page/settings-cards-page.component';
import { SettingsSocialPageComponent }       from './pages/settings-page/settings-social-page/settings-social-page.component';
import { SettingsActivityPageComponent }     from './pages/settings-page/settings-activity-page/settings-activity-page.component';
import { TransparencyPageComponent } from './pages/transparency-page/transparency-page.component';

const appRoutes: Routes = [

  { path: 'signin',                  component: LoginPageComponent },
  { path: 'signin/onToken/:token',   component: LoginPageComponent },  // @todo: remove with controller
  { path: 'signup',                  component: RegisterPageComponent },
  { path: 'signup/success',          component: RegisterSuccessPageComponent },
  { path: 'signup/confirmed/:token', component: RegisterEmailConfirmedPageComponent },
  { path: 'signup/emailTaken',       component: RegisterEmailTakenPageComponent },
  { path: 'signup/2fa',              component: RegisterTfaPageComponent },
  { path: 'signup/2fa/enable',       component: TfaEnablePageComponent },
  { path: 'signup/2fa/skip',         component: TfaNotActivatedPageComponent },
  { path: 'home',     component: HomePageComponent,     canActivate: [AuthGuard] },
  { path: 'transparency',     component: TransparencyPageComponent,     canActivate: [AuthGuard] },
  {
    path: 'account',          component: SettingsPageComponent, canActivate: [AuthGuard],
    children: [
      {path: '', redirectTo: 'profile', pathMatch: 'full'},
      {path: 'profile',       component: SettingsProfilePageComponent},
      {path: 'verification',  component: SettingsVerificationPageComponent},
      {path: '2fa',           component: SettingsTFAPageComponent},
      {path: 'cards',         component: SettingsCardsPageComponent},
      {path: 'cards/:cardId', component: SettingsCardsPageComponent},
      {path: 'social',        component: SettingsSocialPageComponent},
      {path: 'activity',      component: SettingsActivityPageComponent},
      {path: 'limits',        component: LimitsPageComponent}
    ]
  },
  { path: '', redirectTo: 'transparency', pathMatch: 'full' },
  { path: '**', component: NotFoundPageComponent },
];

@NgModule({
  imports: [
    RouterModule.forRoot(
      appRoutes,
      {
        useHash: true,
        enableTracing: false
      }
    )
  ],
  exports: [
    RouterModule
  ],
  providers: []
})
export class AppRouting { }
