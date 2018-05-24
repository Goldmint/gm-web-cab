import {ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnInit} from '@angular/core';
import {MessageBoxService} from "../../services";

@Component({
  selector: 'app-payment-card-block',
  templateUrl: './payment-card-block.component.html',
  styleUrls: ['./payment-card-block.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PaymentCardBlockComponent implements OnInit {

  public agreeCheck: boolean = false;
  public isMobile: boolean = false;

  @Input('amount') estimatedAmount

  constructor(
    private _messageBox: MessageBoxService,
    private cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 767);
    window.onresize = () => {
      this.isMobile = window.innerWidth <= 767 ? true : false;
      this.cdRef.markForCheck();
    };
  }

  onSubmit() {
    this._messageBox.alert('Coming soon');
  }

}
