import { Component, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';
import { DataTablePagerComponent } from '@swimlane/ngx-datatable';

@Component({
  selector: 'app-pager',
  templateUrl: './pager-block.component.html',
  styleUrls: ['./pager-block.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'pager'
  }
})
export class PagerBlockComponent extends DataTablePagerComponent {

  get isVisible(): boolean {
    return this.pages.length > 1;
  }

}
