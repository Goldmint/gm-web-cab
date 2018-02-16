import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

import { UserService } from '../../../services';

@Component({
  selector: 'app-login-ontoken-page',
  templateUrl: './login-ontoken-page.component.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginOntokenPageComponent implements OnInit {

  constructor(
    private _router: Router,
    private _route: ActivatedRoute,
    private _userService: UserService) {

    this._route.params.subscribe(
      params => {
        if (params.token) {
          this._userService.processToken(params.token);
        }
      },
      err => {});
  }

  ngOnInit() {
    this._router.navigate(['/signin']);
  }

}
