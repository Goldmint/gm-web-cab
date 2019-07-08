import { Component, OnInit } from '@angular/core';
import {APIService} from "../../services";

@Component({
  selector: 'app-network-switcher-block',
  templateUrl: './network-switcher-block.component.html',
  styleUrls: ['./network-switcher-block.component.sass']
})
export class NetworkSwitcherBlockComponent implements OnInit {

  public networkList: any;
  public currentNetwork: string;

  constructor(
    private apiService: APIService
  ) { }

  ngOnInit() {
    this.networkList = this.apiService.networkList;
    const network = localStorage.getItem('network');
    this.currentNetwork = network ? network : this.networkList.mainnet;
  }

  changeNetwork(network: string) {
    if (this.currentNetwork == network) return;

    this.currentNetwork = network;
    localStorage.setItem('network', network);
    this.apiService.transferCurrentNetwork.next(network);
  }

}
