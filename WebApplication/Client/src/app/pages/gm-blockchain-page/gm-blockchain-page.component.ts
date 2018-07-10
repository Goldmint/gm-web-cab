import {Component, HostBinding, OnInit, ViewEncapsulation} from '@angular/core';

@Component({
  selector: 'app-gm-blockchain-page',
  templateUrl: './gm-blockchain-page.component.html',
  styleUrls: ['./gm-blockchain-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class GmBlockchainPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  public publicKey: string;
  public walletName: string;

  constructor() { }

  ngOnInit() { }

  onCopyData(input) {
    input.focus();
    input.setSelectionRange(0, input.value.length);
    document.execCommand("copy");
    input.setSelectionRange(0, 0);
  }

  onSubmit() { }

}
