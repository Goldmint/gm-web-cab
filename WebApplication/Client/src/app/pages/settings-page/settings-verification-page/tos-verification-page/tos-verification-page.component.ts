import {ChangeDetectorRef, Component, HostBinding, OnInit} from '@angular/core';
import {APIService, UserService} from "../../../../services";
import {Router} from "@angular/router";

@Component({
  selector: 'app-tos-verification-page',
  templateUrl: './tos-verification-page.component.html',
  styleUrls: ['./tos-verification-page.component.sass']
})
export class TosVerificationPageComponent implements OnInit {

  @HostBinding('class') class = 'page';

  public isAgreeCheck: false;
  public loading = false;

  constructor(
    private _apiService: APIService,
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private _router: Router
  ) { }

  ngOnInit() { }

  agreedWithTos() {
    this.loading = true;
    this._cdRef.markForCheck();

    this._apiService.agreedWithTos().subscribe(data => {
      this._apiService.getProfile().subscribe(res => {
        this.loading = false;
        if (res.data && res.data.verifiedL0) {
          this._router.navigate(['/buy']);
        } else {
          this._router.navigate(['/master-node']);
        }
        this._cdRef.markForCheck();
      }, () => {
        this.loading = false;
        this._cdRef.markForCheck();
      });
    }, () => {
      this.loading = false;
      this._cdRef.markForCheck();
    });
  }

}
