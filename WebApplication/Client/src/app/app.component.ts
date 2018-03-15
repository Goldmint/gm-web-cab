import { Component, ChangeDetectionStrategy, ViewEncapsulation, isDevMode } from '@angular/core';
import {APIService, UserService} from "./services";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {

  constructor(
    private _userService: UserService,
    private _apiService: APIService
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
  }

  ngOnInit() {
    if (isDevMode()) {
      console.info('👋 Development!');
    } else {
      console.info('💪 Production!');
    }

    this._userService.launchTokenRefresher();
  }

}
