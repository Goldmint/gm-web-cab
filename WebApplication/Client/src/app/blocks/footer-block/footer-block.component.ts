import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-footer',
  templateUrl: './footer-block.component.html',
  styleUrls: ['./footer-block.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FooterBlockComponent implements OnInit {

  public year = new Date().getFullYear();

  constructor() { }

  ngOnInit() {
  }

}
