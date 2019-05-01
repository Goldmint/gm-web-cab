import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthGuard } from './guards/index';

import { SellPageComponent } from './pages/sell-page/sell-page.component';
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
import { StaticPagesComponent } from "./pages/static-pages/static-pages.component";
import { LegalSecurityPageComponent } from "./pages/legal-security-page/legal-security-page.component";
import {SettingsFeesPageComponent} from "./pages/settings-page/settings-fees-page/settings-fees-page.component";
import {SellCryptocurrencyPageComponent} from "./pages/sell-page/sell-cryptocurrency-page/sell-cryptocurrency-page.component";
import {SettingsCardsPageComponent} from "./pages/settings-page/settings-cards-page/settings-cards-page.component";
import {MasterNodePageComponent} from "./pages/master-node-page/master-node-page.component";
import {TxInfoPageComponent} from "./pages/scaner-page/tx-info-page/tx-info-page.component";
import {AllBlocksPageComponent} from "./pages/scaner-page/all-blocks-page/all-blocks-page.component";
import {AllTransactionsPageComponent} from "./pages/scaner-page/all-transactions-page/all-transactions-page.component";
import {AddressInfoPageComponent} from "./pages/scaner-page/address-info-page/address-info-page.component";
import {TransactionsInBlockPageComponent} from "./pages/scaner-page/transactions-in-block-page/transactions-in-block-page.component";
import {ScanerPageComponent} from "./pages/scaner-page/scaner-page.component";
import {PawnshopPageComponent} from "./pages/pawnshop-page/pawnshop-page.component";
import {PawnshopFeedPageComponent} from "./pages/pawnshop-page/pawnshop-feed-page/pawnshop-feed-page.component";
import {LatestRewardPageComponent} from "./pages/master-node-page/overview-page/latest-reward-page/latest-reward-page.component";
import {OrganizationsTableComponent} from "./pages/pawnshop-page/pawnshop-feed-page/organizations-table/organizations-table.component";
import {PawnshopsTableComponent} from "./pages/pawnshop-page/pawnshop-feed-page/pawnshops-table/pawnshops-table.component";
import {FeedTableComponent} from "./pages/pawnshop-page/pawnshop-feed-page/feed-table/feed-table.component";
import {AllTicketFeedPageComponent} from "./pages/pawnshop-page/pawnshop-feed-page/all-ticket-feed-page/all-ticket-feed-page.component";
import {RewardTransactionsPageComponent} from "./pages/master-node-page/overview-page/reward-transactions-page/reward-transactions-page.component";
import {BlockchainPoolPageComponent} from "./pages/blockchain-pool-page/blockchain-pool-page.component";
import {HoldTokensPageComponent} from "./pages/blockchain-pool-page/hold-tokens-page/hold-tokens-page.component";
import {OverviewPageComponent} from "./pages/master-node-page/overview-page/overview-page.component";
import {LaunchNodePageComponent} from "./pages/master-node-page/launch-node-page/launch-node-page.component";


const appRoutes: Routes = [
  { path: 'signin', component: LoginPageComponent },
  { path: 'signin/onToken/:token', component: LoginPageComponent },
  { path: 'signin/restore', component: PasswordResetPageComponent },
  { path: 'signin/restore/:token', component: PasswordResetPageComponent },
  { path: 'signin/dpa/required', component: LoginDpaRequiredComponent },
  { path: 'signin/dpa/signed/:token', component: LoginDpaSignedComponent },
  { path: 'signup', component: RegisterPageComponent },
  { path: 'signup/success', component: RegisterSuccessPageComponent },
  { path: 'signup/confirmed/:token', component: RegisterEmailConfirmedPageComponent },
  { path: 'signup/emailTaken', component: RegisterEmailTakenPageComponent },
  { path: 'signup/2fa', component: RegisterTfaPageComponent },
  { path: 'master-node', component: LaunchNodePageComponent},
  { path: 'nodes', component: OverviewPageComponent },
  { path: 'nodes/latest-reward-distributions', component: LatestRewardPageComponent },
  { path: 'nodes/reward-transactions/:id', component: RewardTransactionsPageComponent },
  { path: 'sell', component: SellPageComponent },
  { path: 'sell/cryptocarrency', component: SellCryptocurrencyPageComponent, canActivate: [AuthGuard] },
  { path: 'legal-security', component: LegalSecurityPageComponent },
  { path: 'legal-security/:page', component: StaticPagesComponent },
  {
    path: 'finance', component: FinancePageComponent,
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
      // { path: 'limits', component: LimitsPageComponent }
      // { path: 'fees', component: SettingsFeesPageComponent }
    ]
  },
  { path: 'ethereum-pool', component: BlockchainPoolPageComponent },
  { path: 'ethereum-pool/hold-tokens', component: HoldTokensPageComponent },
  { path: 'scanner', component: ScanerPageComponent },
  { path: 'scanner/tx/:id', component: TxInfoPageComponent },
  { path: 'scanner/address/:id', component: AddressInfoPageComponent },
  { path: 'scanner/blocks', component: AllBlocksPageComponent },
  { path: 'scanner/transactions', component: AllTransactionsPageComponent },
  { path: 'scanner/transactions-in-block/:id', component: TransactionsInBlockPageComponent },
  { path: 'pawnshop-loans', component: PawnshopPageComponent,
    children: [
      { path: '', redirectTo: 'feed', pathMatch: 'full' },
      { path: 'feed', component: PawnshopFeedPageComponent,
        children: [
          { path: 'all-ticket-feed', component: AllTicketFeedPageComponent },
          { path: 'organizations', component: OrganizationsTableComponent },
          { path: 'pawnshop/:id', component: PawnshopsTableComponent },
          { path: 'organization-feed/:id', component: FeedTableComponent },
        ]
      }
    ]
  },

  { path: '', redirectTo: 'master-node', pathMatch: 'full' },
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
