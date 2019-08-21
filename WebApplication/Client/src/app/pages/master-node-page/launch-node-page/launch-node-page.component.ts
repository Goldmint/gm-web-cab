import {Component, HostBinding, OnInit} from '@angular/core';
import {environment} from "../../../../environments/environment";

@Component({
  selector: 'app-launch-node-page',
  templateUrl: './launch-node-page.component.html',
  styleUrls: ['./launch-node-page.component.sass']
})
export class LaunchNodePageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  public liteWalletLink;

  private liteWallet;

  constructor() { }

  ngOnInit() {
    this.liteWallet = window['GoldMint'];
    let isFirefox = typeof window['InstallTrigger'] !== 'undefined';
    this.liteWalletLink = isFirefox ? environment.getLiteWalletLink.firefox : environment.getLiteWalletLink.chrome;
  }
}
