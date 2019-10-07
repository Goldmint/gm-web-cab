import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { NotFoundPageComponent } from './pages/not-found-page/not-found-page.component';
import { StaticPagesComponent } from "./pages/static-pages/static-pages.component";
import { LegalSecurityPageComponent } from "./pages/legal-security-page/legal-security-page.component";
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
import {BuyMntpPageComponent} from "./pages/buy-mntp-page/buy-mntp-page.component";
import {SwapMntpComponent} from "./pages/swap-mntp/swap-mntp.component";


const appRoutes: Routes = [
  { path: 'master-node', component: LaunchNodePageComponent},
  { path: 'nodes', component: OverviewPageComponent },
  { path: 'nodes/latest-reward-distributions', component: LatestRewardPageComponent },
  { path: 'nodes/reward-transactions/:id', component: RewardTransactionsPageComponent },
  { path: 'legal-security', component: LegalSecurityPageComponent },
  { path: 'legal-security/:page', component: StaticPagesComponent },
  { path: 'ethereum-pool', component: BlockchainPoolPageComponent },
  { path: 'ethereum-pool/hold-tokens', component: HoldTokensPageComponent },
  { path: 'buy-mntp', component: BuyMntpPageComponent },
  // { path: 'swap-mntp', component: SwapMntpComponent },
  { path: 'scanner', component: ScanerPageComponent },
  { path: 'scanner/tx/:id', component: TxInfoPageComponent },
  { path: 'scanner/tx/:id/:network', component: TxInfoPageComponent },
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
