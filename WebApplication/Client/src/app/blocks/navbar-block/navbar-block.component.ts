import {Component, OnInit, ChangeDetectionStrategy, Output, EventEmitter} from '@angular/core';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar-block.component.html',
  styleUrls: ['./navbar-block.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavbarBlockComponent implements OnInit {

  @Output() onChanged = new EventEmitter<boolean>();

  constructor() { }

  ngOnInit() { }

  hideMobileMenu(status: boolean) {
    this.onChanged.emit(status);
  }

}
