import { Component, OnInit } from '@angular/core';
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-master-node-page',
  templateUrl: './master-node-page.component.html',
  styleUrls: ['./master-node-page.component.sass']
})
export class MasterNodePageComponent implements OnInit {

  public switchModel: {
    type: 'overview'|'launch'
  };
  public isProduction = environment.isProduction;
  public getLiteWalletLink;

  constructor() { }

  ngOnInit() {
    let isFirefox = typeof window['InstallTrigger'] !== 'undefined';
    this.getLiteWalletLink = isFirefox ? environment.getLiteWalletLink.firefox : environment.getLiteWalletLink.chrome;

    this.switchModel = {
      type: 'overview'
    };
  }

  goToMigration(value) {
    this.switchModel = {type: value};
  }

}
