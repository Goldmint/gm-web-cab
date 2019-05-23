import {Component, OnInit } from '@angular/core';
import { ActivatedRoute } from "@angular/router";

enum Pages {termsOfSale, privacy, kycpolicy, termsOfTesting}
let linksArray:[string] = [
  'https://www.goldmint.io/uploads/Gold_Coin_Terms_of_sale.pdf',
  'https://www.goldmint.io/uploads/Consumer_data_privacy_Policy.pdf',
  'https://www.goldmint.io/uploads/KYC_AML_Policy.pdf',
  'https://www.goldmint.io/wp-content/uploads/2019/05/Testing-terms-Eng-v1.pdf'
];

@Component({
  selector: 'app-static-pages',
  templateUrl: './static-pages.component.html',
  styleUrls: ['./static-pages.component.sass']
})
export class StaticPagesComponent implements OnInit {
  public pagePath:string;
  private _pages = Pages;
  private _links = linksArray;

  constructor(
      private _route: ActivatedRoute,
  ) {
    this._route.params
        .subscribe(params => {
          let page = params.page;
          if (page) {
            this.pagePath = this._links[this._pages[page]];
          }
        })
  }

  ngOnInit() {
  }

}
