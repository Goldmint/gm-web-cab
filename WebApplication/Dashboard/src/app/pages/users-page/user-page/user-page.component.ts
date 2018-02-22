import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {Page} from "../../../models/page";
import {Subscription} from "rxjs/Subscription";
import {APIService, UserService} from "../../../services";
import {TranslateService} from "@ngx-translate/core";
import {ActivatedRoute} from "@angular/router";

@Component({
  selector: 'app-user-page',
  templateUrl: './user-page.component.html',
  styleUrls: ['./user-page.component.sass']
})
export class UserPageComponent implements OnInit, OnDestroy {

  public locale: string;
  public loading: boolean;
  public page = new Page();

  public sub1: Subscription;
  public currentUserProperties = [];
  public currentUserName: string;

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    public translate: TranslateService,
    private route: ActivatedRoute
  ) {

  }

  ngOnInit() {
    this.userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this.sub1 = this.route.params.subscribe(params => {
      this.apiService.getUsersAccountInfo(params.id).subscribe(data => {
        this.currentUserProperties = data.data.properties;
        this.cdRef.detectChanges();
      });
    });

  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }

}
