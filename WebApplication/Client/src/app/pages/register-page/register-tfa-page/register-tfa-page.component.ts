import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';
import { Router } from "@angular/router";
import { APIService } from "../../../services";

enum Page {Default, KeepDisabled };

@Component({
  selector: 'app-register-tfa-page',
  templateUrl: './register-tfa-page.component.html',
  styleUrls: ['./register-tfa-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterTfaPageComponent implements OnInit {

  private page: Page;
  private pages = Page;

  constructor(
    private router: Router,
    private apiService: APIService
    ) {
    this.page = Page.Default;
  }

  ngOnInit() {
  }

  setPage(page: Page) {
    this.page = page;
  }

  keepDisabled() {
    this.apiService.verifyTFACode("000000", false).subscribe();
    this.router.navigate([ "/" ]);
  }

}
