import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-payment-card-block',
  templateUrl: './payment-card-block.component.html',
  styleUrls: ['./payment-card-block.component.sass']
})
export class PaymentCardBlockComponent implements OnInit {

  public agreeCheck: boolean = false;

  constructor() { }

  ngOnInit() {
  }

}
