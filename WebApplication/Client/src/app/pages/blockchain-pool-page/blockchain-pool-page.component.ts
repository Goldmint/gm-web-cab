import {Component, HostBinding, OnInit} from '@angular/core';
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-blockchain-pool-page',
  templateUrl: './blockchain-pool-page.component.html',
  styleUrls: ['./blockchain-pool-page.component.sass']
})
export class BlockchainPoolPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  public isProduction = environment.isProduction;

  constructor() { }

  ngOnInit() {
  }

}
