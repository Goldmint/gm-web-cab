import { NgModule } from '@angular/core';
import { RouterModule, Routes, CanActivate } from '@angular/router';

import { AuthGuard } from './guards/index';

import { HomePageComponent }     from './pages/home-page/home-page.component';
import { NotFoundPageComponent } from './pages/not-found-page/not-found-page.component';
import { LoginPageComponent }         from './pages/login-page/login-page.component';
// import { LoginOntokenPageComponent }  from './pages/login-page/login-ontoken-page/login-ontoken-page.component';
import { RegisterPageComponent } from './pages/register-page/register-page.component';
import { RegisterTfaPageComponent } from './pages/register-page/register-tfa-page/register-tfa-page.component';
import { RegisterSuccessPageComponent } from './pages/register-page/register-success-page/register-success-page.component';
import { RegisterEmailTakenPageComponent } from './pages/register-page/register-email-taken-page/register-email-taken-page.component';
import { RegisterEmailConfirmedPageComponent } from './pages/register-page/register-email-confirmed-page/register-email-confirmed-page.component';
import { TfaEnablePageComponent }       from './pages/register-page/register-tfa-page/tfa-enable-page/tfa-enable-page.component';
import { TfaNotActivatedPageComponent } from './pages/register-page/register-tfa-page/tfa-not-activated-page/tfa-not-activated-page.component';
import { TransparencyPageComponent } from './pages/transparency-page/transparency-page.component';
import { CountriesPageComponent } from "./pages/countries-page/countries-page.component";
import {UsersPageComponent} from "./pages/users-page/users-page.component";
import {UsersListPageComponent} from "./pages/users-page/users-list-page/users-list-page.component";
import {UserPageComponent} from "./pages/users-page/user-page/user-page.component";
import {OplogPageComponent} from "./pages/users-page/oplog-page/oplog-page.component";
import {AccessRightsPageComponent} from "./pages/users-page/access-rights-page/access-rights-page.component";

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
  { path: 'countries',     component: CountriesPageComponent,     canActivate: [AuthGuard] },
  { path: 'users',     component: UsersPageComponent,     canActivate: [AuthGuard],
    children: [
      {path: '', component: UsersListPageComponent},
      {path: ':id', component: UserPageComponent},
      {path: ':id/oplog', component: OplogPageComponent},
      {path: ':id/access', component: AccessRightsPageComponent}
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
