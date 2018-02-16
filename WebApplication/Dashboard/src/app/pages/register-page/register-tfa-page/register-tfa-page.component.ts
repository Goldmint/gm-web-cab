import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';

enum Page {Default, Enable2FA, NotActivated, Disabled};

@Component({
  selector: 'app-register-tfa-page',
  templateUrl: './register-tfa-page.component.html',
  styleUrls: ['./register-tfa-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterTfaPageComponent implements OnInit {

  public page: Page;

  constructor() {
    this.page = Page.Default;
  }

  ngOnInit() {
  }

  setPage(page: Page) {
    this.page = page;
  }

}
