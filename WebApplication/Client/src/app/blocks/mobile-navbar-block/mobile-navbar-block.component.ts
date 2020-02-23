import { Component, OnInit } from '@angular/core';
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-mobile-navbar-block',
  templateUrl: './mobile-navbar-block.component.html',
  styleUrls: ['./mobile-navbar-block.component.sass']
})
export class MobileNavbarBlockComponent implements OnInit {

  public getLiteWalletLink;

  constructor() { }

  ngOnInit() {
    let isFirefox = typeof window['InstallTrigger'] !== 'undefined';
    this.getLiteWalletLink = isFirefox ? environment.getLiteWalletLink.firefox : environment.getLiteWalletLink.chrome;
  }

}
