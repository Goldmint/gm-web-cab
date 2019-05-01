import {Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef} from '@angular/core';
import {CommonService} from "../../services/common.service";
import {Subscription} from "rxjs";

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar-block.component.html',
  styleUrls: ['./navbar-block.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavbarBlockComponent implements OnInit, OnDestroy {

  public activeMenuItem: string;

  private sub1: Subscription;

  constructor(
    private commonService: CommonService,
    private cdRef: ChangeDetectorRef,
  ) { }

  ngOnInit() {
    this.sub1 = this.commonService.getActiveMenuItem.subscribe(res => {
      if (res !== null) {
        this.activeMenuItem = res;
        this.cdRef.markForCheck();
      }
    });
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }
}
