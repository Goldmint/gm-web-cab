import {Component, ChangeDetectionStrategy, ViewEncapsulation, isDevMode, ChangeDetectorRef} from '@angular/core';
import {APIService, UserService} from "./services";
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
    private _userService: UserService,
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef
  ) {
    let queryParams: any = {};
    let regexp = /[?&]([\w-]+)=([^?&]*)/g;
    for (let matches; (matches = regexp.exec(window.location.search)) !== null; queryParams[matches[1]] = matches[2]);

    this._userService.currentUser.subscribe(data => {
      if (data.id && queryParams.zendeskAuth && queryParams.return_to) {
        this._apiService.getZendeskTokenSSO().subscribe(token => {
          window.location.replace(`https://goldmint.zendesk.com/access/jwt?jwt=${token.data.jwt}&return_to=${ queryParams.return_to }`);
        });
      }
    });

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

    this._userService.launchTokenRefresher();
  }

  acceptCookies() {
    localStorage.setItem('cookies_info', 'true');
    this.showCookiesMessage = false;
    this._cdRef.markForCheck();
  }

}
