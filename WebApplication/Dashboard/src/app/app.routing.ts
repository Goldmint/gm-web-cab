import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthGuard } from './guards/index';

import { NotFoundPageComponent } from './pages/not-found-page/not-found-page.component';
import { LoginPageComponent }         from './pages/login-page/login-page.component';
import { SettingsPageComponent }             from './pages/settings-page/settings-page.component';
import { TransparencyPageComponent } from './pages/transparency-page/transparency-page.component';
import { CountriesPageComponent } from "./pages/countries-page/countries-page.component";
import {UsersPageComponent} from "./pages/users-page/users-page.component";
import {UsersListPageComponent} from "./pages/users-page/users-list-page/users-list-page.component";
import {UserPageComponent} from "./pages/users-page/user-page/user-page.component";
import {OplogPageComponent} from "./pages/users-page/oplog-page/oplog-page.component";
import {AccessRightsPageComponent} from "./pages/users-page/access-rights-page/access-rights-page.component";
import {SettingsProfilePageComponent} from "./pages/settings-page/settings-profile-page/settings-profile-page.component";
import {FeesPageComponent} from "./pages/fees-page/fees-page.component";
import {SwiftPageComponent} from "./pages/swift-page/swift-page.component";

const appRoutes: Routes = [

  { path: 'signin', component: LoginPageComponent },
  { path: 'transparency', component: TransparencyPageComponent, canActivate: [AuthGuard] },
  { path: 'swift', component: SwiftPageComponent, canActivate: [AuthGuard] },
  { path: 'countries', component: CountriesPageComponent, canActivate: [AuthGuard] },
  { path: 'fees', component: FeesPageComponent, canActivate: [AuthGuard] },
  { path: 'users', component: UsersPageComponent, canActivate: [AuthGuard],
    children: [
      {path: '', component: UsersListPageComponent},
      {path: ':id', component: UserPageComponent},
      {path: ':id/oplog', component: OplogPageComponent},
      {path: ':id/access', component: AccessRightsPageComponent}
    ]
  },
  {
    path: 'account', component: SettingsPageComponent, canActivate: [AuthGuard],
    children: [
      {path: '', redirectTo: 'profile', pathMatch: 'full'},
      {path: 'profile', component: SettingsProfilePageComponent},
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
