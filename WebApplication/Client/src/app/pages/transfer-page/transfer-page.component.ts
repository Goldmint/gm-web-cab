import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, HostBinding, TemplateRef, ChangeDetectorRef } from '@angular/core';
import { BsModalService } from 'ngx-bootstrap/modal';
import { BsModalRef } from 'ngx-bootstrap/modal/bs-modal-ref.service';
import { MessageBoxService, EthereumService } from "../../services/index";
import { BigNumber } from 'bignumber.js'

@Component({
  selector: 'app-transfer-page',
  templateUrl: './transfer-page.component.html',
  styleUrls: ['./transfer-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransferPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  private _modalRef: BsModalRef;
  public amount: BigNumber = new BigNumber(0);
  public walletAddressRaw: string = null;
  public walletAddress: string = null;

  constructor(
    private _modalService: BsModalService,
    private _ethService: EthereumService,
    private _messageBox: MessageBoxService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
  }

  modal(template: TemplateRef<any>) {
    if (this._modalRef) {
      this._modalRef.hide();
    }
    this._modalRef = this._modalService.show(template, { class: 'modal-lg' });
  }

  onWalletAddressChanged(value: string) {
    this.walletAddress = null;
    if (this._ethService.isValidAddress(value)) {
      this.walletAddress = value;
    }
    this._cdRef.detectChanges();
  }

  onAmountChanged(value: string) {
    this.amount = new BigNumber(0);
    if (value != '') {
      this.amount = new BigNumber(value);
      this.amount = this.amount.decimalPlaces(6, BigNumber.ROUND_DOWN);
    }
    this._cdRef.detectChanges();
  }

  onMetamask() {
    var ethAddress = this._ethService.getEthAddress();
    if (ethAddress == null) {
      this._messageBox.alert('Enable metamask first');
      return;
    }

    var confText =
      "Target address: " + this.walletAddress + "<br/>" +
      "GOLD amount: " + this.amount + " GOLD<br/>"
      ;
    this._messageBox.confirm(confText).subscribe(ok => {
      if (ok) {
        this._ethService.transferGoldToWallet(ethAddress, this.walletAddress, this.amount);
        this.walletAddressRaw = "";
        this.amount = new BigNumber(0);
      }
      this._cdRef.detectChanges();
    });
  }
}
