import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-master-node-page',
  templateUrl: './master-node-page.component.html',
  styleUrls: ['./master-node-page.component.sass']
})
export class MasterNodePageComponent implements OnInit {

  public switchModel: {
    type: 'overview'|'launch'|'migration'
  };

  constructor() { }

  ngOnInit() {
    this.switchModel = {
      type: 'migration'
    };
  }

  goToMigration(value) {
    this.switchModel = {type: value};
  }

}
