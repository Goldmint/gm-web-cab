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

  changeNetwork(networ: string) {
    if (this.currentNetwork == networ) return;

    this.currentNetwork = networ;
    localStorage.setItem('network', networ);
    this.apiService.transferCurrentNetwork.next(networ);
  }

}
