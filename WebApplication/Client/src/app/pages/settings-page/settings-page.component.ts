import {Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef} from '@angular/core';
import {APIService} from "../../services";
import {User} from "../../interfaces/user";
import {environment} from "../../../environments/environment";

@Component({
  selector: 'app-settings-page',
  templateUrl: './settings-page.component.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsPageComponent implements OnInit {

  public user: User;
  public hasExtraRights: boolean = true;
  public loading: boolean = true;

  constructor(
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this._apiService.getProfile().subscribe(data => {
      this.user = data.data;

      if (environment.detectExtraRights) {
        this.hasExtraRights = this.user.hasExtraRights;
      }
      this.loading = false;
      this._cdRef.markForCheck();
    });
  }

}
