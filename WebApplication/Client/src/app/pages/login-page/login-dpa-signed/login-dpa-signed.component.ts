import {ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {ActivatedRoute, Router} from "@angular/router";
import {APIService, UserService} from "../../../services";
import {Subscription} from "rxjs/Subscription";
import {Observable} from "rxjs/Observable";

@Component({
  selector: 'app-login-dpa-signed',
  templateUrl: './login-dpa-signed.component.html',
  styleUrls: ['./login-dpa-signed.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginDpaSignedComponent implements OnInit, OnDestroy {

  private interval: Subscription;
  private token: string;

  constructor(
    private _apiService: APIService,
    private _userService: UserService,
    private route: ActivatedRoute,
    private router: Router,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.token = params['token'];
      this.dpaCheck();
    });
  }

  dpaCheck() {
    this.interval && this.interval.unsubscribe();

    this._apiService.dpaCheck(this.token)
      .finally(() => {
        this._cdRef.markForCheck();
      }).subscribe((data) => {
        let userSubscription = this._userService.currentUser.subscribe(() => {
          userSubscription.unsubscribe();
          this.router.navigate(['/']);
        });
      this._userService.processToken(data.data.token);
    }, (error) => {
      if (error.error.errorCode === 1011) {
        this.interval = Observable.interval(5000).subscribe(() => {
          this.dpaCheck();
        });
      } else {
        this.router.navigate(['/signin']);
      }
    });
  }

  ngOnDestroy() {
    this.interval && this.interval.unsubscribe();
  }

}
