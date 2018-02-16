import { Component, ChangeDetectionStrategy, ViewEncapsulation, isDevMode } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { UserService } from "./services";

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
    private _translateService: TranslateService) {
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
