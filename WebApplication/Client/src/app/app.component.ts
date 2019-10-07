import {Component, ChangeDetectionStrategy, ViewEncapsulation, ChangeDetectorRef} from '@angular/core';
import {Version} from "../version";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {

  public appVersion: string = "";
  public showCookiesMessage: boolean = false;

  constructor(
    private _cdRef: ChangeDetectorRef
  ) {
    document.addEventListener('focusout', event => {
      if (event.target instanceof HTMLInputElement) {
        let value = event.target.value;
        event.target.value = value.trim();
        if (value !== event.target.value) {
          var evt = document.createEvent('HTMLEvents');
          evt.initEvent('input', true, true);
          event.target.dispatchEvent(evt);
        }
      }
    });
	console.log(Version);
	this.appVersion = Version.commit + " / " + Version.branch;
  }

  ngOnInit() {
    if (!localStorage.getItem('cookies_info')) {
      this.showCookiesMessage = true;
      this._cdRef.markForCheck();
    }
  }

  acceptCookies() {
    localStorage.setItem('cookies_info', 'true');
    this.showCookiesMessage = false;
    this._cdRef.markForCheck();
  }

}
