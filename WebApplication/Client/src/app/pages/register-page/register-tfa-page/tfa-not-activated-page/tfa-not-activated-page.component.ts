import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';

enum Pages {TFANotActivated, TFADisabled};

@Component({
  selector: 'app-tfa-not-activated-page',
  templateUrl: './tfa-not-activated-page.component.html',
  styleUrls: ['./tfa-not-activated-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TfaNotActivatedPageComponent implements OnInit {

  public pages = Pages;

  public page: Pages;

  constructor() {
    this.page = Pages.TFANotActivated;
  }

  ngOnInit() {
  }

}
