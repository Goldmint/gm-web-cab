import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-settings-page',
  templateUrl: './settings-page.component.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsPageComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
