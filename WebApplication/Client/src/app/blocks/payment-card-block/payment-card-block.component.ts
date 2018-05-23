import {Component, Input, OnInit} from '@angular/core';
import {MessageBoxService} from "../../services";

@Component({
  selector: 'app-payment-card-block',
  templateUrl: './payment-card-block.component.html',
  styleUrls: ['./payment-card-block.component.sass']
})
export class PaymentCardBlockComponent implements OnInit {

  public agreeCheck: boolean = false;

  @Input('amount') estimatedAmount

  constructor(
    private _messageBox: MessageBoxService
  ) { }

  ngOnInit() { }

  onSubmit() {
    this._messageBox.alert('Coming soon');
  }

}
