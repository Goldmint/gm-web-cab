import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar-block.component.html',
  styleUrls: ['./navbar-block.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavbarBlockComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
