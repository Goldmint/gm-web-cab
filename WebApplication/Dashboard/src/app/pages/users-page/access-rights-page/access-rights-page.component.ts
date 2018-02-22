import {ChangeDetectorRef, Component, OnInit, OnDestroy} from '@angular/core';
import {Page} from "../../../models/page";
import {Subscription} from "rxjs/Subscription";
import {APIService, UserService} from "../../../services";
import {TranslateService} from "@ngx-translate/core";
import {ActivatedRoute} from "@angular/router";


@Component({
  selector: 'app-access-rights-page',
  templateUrl: './access-rights-page.component.html',
  styleUrls: ['./access-rights-page.component.sass']
})
export class AccessRightsPageComponent implements OnInit, OnDestroy {

  public locale: string;
  public page = new Page();

  public sub1: Subscription;
  public currentUser = {};
  public accessRights = [];
  public emptyUserMask = 'u00000X';
  public userId;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.sub1 = this.route.params.subscribe(params => {
      this.userId = params.id;

      this.apiService.getUsersAccountInfo(params.id).subscribe(data => {
        this.accessRights = data.data.accessRights;
        this.accessRights.forEach(item => {
          item.title = item.n.replace(/([a-z])(?=[A-Z])/g, '$1 ');
        });
        this.cdRef.detectChanges();
      });
    });

    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

  }

  save() {
    let mask = 0;
    this.accessRights.forEach(item => item.c && (mask |= item.m));
    this.apiService.setUserAccessRight(this.userId, mask).subscribe();
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }

}
