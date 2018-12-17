import { Component, OnInit } from '@angular/core';
import {APIService} from "../../services";
import {CommonService} from "../../services/common.service";

@Component({
  selector: 'app-pawnshop-page',
  templateUrl: './pawnshop-page.component.html',
  styleUrls: ['./pawnshop-page.component.sass']
})
export class PawnshopPageComponent implements OnInit {

  constructor(
    private apiService: APIService,
    private commonService: CommonService
  ) { }

  ngOnInit() {
    this.apiService.getOrganizationsName().subscribe((data: any) => {
      this.commonService.getPawnShopOrganization.next(data.res.list);
    });
  }

}
