import {Component, HostBinding, OnInit} from '@angular/core';
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-wallet-page',
  templateUrl: './wallet-page.component.html',
  styleUrls: ['./wallet-page.component.sass']
})
export class WalletPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  public getLiteWalletLink = environment.getLiteWalletLink;
  public isProduction = environment.isProduction;

  constructor() { }

  ngOnInit() {
  }

}
