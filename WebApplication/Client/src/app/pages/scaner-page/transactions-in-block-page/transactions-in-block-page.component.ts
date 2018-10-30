import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {APIService, UserService} from "../../../services";
import {Subject} from "rxjs/Subject";
import {ActivatedRoute} from "@angular/router";
import {Page} from "../../../models/page";
import {Block} from "../../../interfaces/block";

@Component({
  selector: 'app-transactions-in-block-page',
  templateUrl: './transactions-in-block-page.component.html',
  styleUrls: ['./transactions-in-block-page.component.sass']
})
export class TransactionsInBlockPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public page = new Page();
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public blockNumber: number;
  public block: Block;

  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private apiService: APIService,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.route.params.takeUntil(this.destroy$).subscribe(params => {
      this.blockNumber = params.id;
      this.setPage();
    });
  }

  setPage() {
    this.loading = true;

    this.apiService.getTransactionsInBlock(this.blockNumber)
      .finally(() => {
        this.loading = false;
        this.isDataLoaded = true;
        this.cdRef.markForCheck();
      })
      .subscribe((data: any) => {
        this.block = data.res;
      });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }
}
