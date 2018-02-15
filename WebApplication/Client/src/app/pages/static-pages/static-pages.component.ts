import {Component, OnInit } from '@angular/core';
import { ActivatedRoute } from "@angular/router";

enum Pages {termsOfUse, privacy, kycpolicy}
let linksArray:[string] = [
    'http://www.pdf995.com/samples/pdf.pdf',
    'http://unec.edu.az/application/uploads/2014/12/pdf-sample.pdf',
    'http://gahp.net/wp-content/uploads/2017/09/sample.pdf',
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
