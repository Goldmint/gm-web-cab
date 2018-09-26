import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthGuard } from './guards/index';

import { HomePageComponent } from './pages/home-page/home-page.component';
import { SellPageComponent } from './pages/sell-page/sell-page.component';
import { BuyPageComponent } from './pages/buy-page/buy-page.component';
import { FinancePageComponent } from './pages/finance-page/finance-page.component';
import { HistoryPageComponent } from './pages/history-page/history-page.component';
import { LimitsPageComponent } from './pages/limits-page/limits-page.component';
import { SupportPageComponent } from './pages/support-page/support-page.component';
import { NotFoundPageComponent } from './pages/not-found-page/not-found-page.component';
import { LoginPageComponent } from './pages/login-page/login-page.component';
import { PasswordResetPageComponent } from './pages/login-page/password-reset-page/password-reset-page.component';
import { LoginDpaRequiredComponent } from "./pages/login-page/login-dpa-required/login-dpa-required.component";
import { LoginDpaSignedComponent } from "./pages/login-page/login-dpa-signed/login-dpa-signed.component";
import { RegisterPageComponent } from './pages/register-page/register-page.component';
import { RegisterTfaPageComponent } from './pages/register-page/register-tfa-page/register-tfa-page.component';
import { RegisterSuccessPageComponent } from './pages/register-page/register-success-page/register-success-page.component';
import { RegisterEmailTakenPageComponent } from './pages/register-page/register-email-taken-page/register-email-taken-page.component';
import { RegisterEmailConfirmedPageComponent } from './pages/register-page/register-email-confirmed-page/register-email-confirmed-page.component';
import { SettingsPageComponent } from './pages/settings-page/settings-page.component';
import { SettingsProfilePageComponent } from './pages/settings-page/settings-profile-page/settings-profile-page.component';
import { SettingsVerificationPageComponent } from './pages/settings-page/settings-verification-page/settings-verification-page.component';
import { SettingsTFAPageComponent } from './pages/settings-page/settings-tfa-page/settings-tfa-page.component';
import { SettingsSocialPageComponent } from './pages/settings-page/settings-social-page/settings-social-page.component';
import { SettingsActivityPageComponent } from './pages/settings-page/settings-activity-page/settings-activity-page.component';
import { TransparencyPageComponent } from './pages/transparency-page/transparency-page.component';
import { StaticPagesComponent } from "./pages/static-pages/static-pages.component";
import { LegalSecurityPageComponent } from "./pages/legal-security-page/legal-security-page.component";
import {SettingsFeesPageComponent} from "./pages/settings-page/settings-fees-page/settings-fees-page.component";
import {BuyCryptocurrencyPageComponent} from "./pages/buy-page/buy-cryptocurrency-page/buy-cryptocurrency-page.component";
import {SellCryptocurrencyPageComponent} from "./pages/sell-page/sell-cryptocurrency-page/sell-cryptocurrency-page.component";
import {SettingsCardsPageComponent} from "./pages/settings-page/settings-cards-page/settings-cards-page.component";
import {BuyCardPageComponent} from "./pages/buy-page/buy-card-page/buy-card-page.component";
import {SellCardPageComponent} from "./pages/sell-page/sell-card-page/sell-card-page.component";
import {TransferPageComponent} from "./pages/transfer-page/transfer-page.component";


const appRoutes: Routes = [
  { path: 'signin', component: LoginPageComponent },
  { path: 'signin/onToken/:token', component: LoginPageComponent },  // @todo: remove with controller
  { path: 'signin/restore', component: PasswordResetPageComponent },
  { path: 'signin/restore/:token', component: PasswordResetPageComponent },
  { path: 'signin/dpa/required', component: LoginDpaRequiredComponent },
  { path: 'signin/dpa/signed/:token', component: LoginDpaSignedComponent },
  { path: 'signup', component: RegisterPageComponent },
  { path: 'signup/success', component: RegisterSuccessPageComponent },
  { path: 'signup/confirmed/:token', component: RegisterEmailConfirmedPageComponent },
  { path: 'signup/emailTaken', component: RegisterEmailTakenPageComponent },
  { path: 'signup/2fa', component: RegisterTfaPageComponent },
  { path: 'home', component: HomePageComponent, canActivate: [AuthGuard] },
  { path: 'buy', component: BuyPageComponent, canActivate: [AuthGuard] },
  { path: 'buy/cryptocarrency', component: BuyCryptocurrencyPageComponent, canActivate: [AuthGuard] },
  { path: 'buy/payment-card', component: BuyCardPageComponent, canActivate: [AuthGuard] },
  { path: 'sell', component: SellPageComponent, canActivate: [AuthGuard] },
  { path: 'sell/cryptocarrency', component: SellCryptocurrencyPageComponent, canActivate: [AuthGuard] },
  { path: 'sell/payment-card', component: SellCardPageComponent, canActivate: [AuthGuard] },
  { path: 'transfer', component: TransferPageComponent, canActivate: [AuthGuard] },
  { path: 'legal-security', component: LegalSecurityPageComponent },
  { path: 'legal-security/:page', component: StaticPagesComponent },
  {
    path: 'finance', component: FinancePageComponent, canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: 'history', pathMatch: 'full' },
      { path: 'history', component: HistoryPageComponent }
    ]
  },
  { path: 'support', component: SupportPageComponent, canActivate: [AuthGuard] },
  {
    path: 'account', component: SettingsPageComponent, canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: 'profile', pathMatch: 'full' },
      { path: 'profile', component: SettingsProfilePageComponent },
      { path: 'verification', component: SettingsVerificationPageComponent },
      { path: '2fa', component: SettingsTFAPageComponent },
      { path: 'cards', component: SettingsCardsPageComponent },
      { path: 'cards/:cardId', component: SettingsCardsPageComponent },
      { path: 'social', component: SettingsSocialPageComponent },
      { path: 'activity', component: SettingsActivityPageComponent },
      { path: 'limits', component: LimitsPageComponent }
      // { path: 'fees', component: SettingsFeesPageComponent }
    ]
  },
  { path: 'transparency', component: TransparencyPageComponent },
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: '**', component: NotFoundPageComponent }
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
