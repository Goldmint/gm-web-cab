import { Component, OnInit } from '@angular/core';
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-master-node-page',
  templateUrl: './master-node-page.component.html',
  styleUrls: ['./master-node-page.component.sass']
})
export class MasterNodePageComponent implements OnInit {

  public switchModel: {
    type: 'overview'|'launch'|'migration'
  };
  public isProduction = environment.isProduction;

  constructor() { }

  ngOnInit() {
    this.switchModel = {
      type: 'overview'
    };
  }

  goToMigration(value) {
    this.switchModel = {type: value};
  }

}
