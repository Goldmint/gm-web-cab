import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-register-email-taken-page',
  templateUrl: './register-email-taken-page.component.html',
  styleUrls: ['./register-email-taken-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterEmailTakenPageComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
