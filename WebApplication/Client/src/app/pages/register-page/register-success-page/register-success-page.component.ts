import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-register-success-page',
  templateUrl: './register-success-page.component.html',
  styleUrls: ['./register-success-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterSuccessPageComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
