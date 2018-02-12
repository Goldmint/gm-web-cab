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
  private _amount: BigNumber = new BigNumber(0);
  private _walletAddressRaw: string = null;
  private _walletAddress: string = null;

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
    this._walletAddress = null;
    if (this._ethService.isValidAddress(value)) {
      this._walletAddress = value;
    }
    this._cdRef.detectChanges();
  }

  onAmountChanged(value: string) {
    this._amount = new BigNumber(0);
    if (value != '') {
      this._amount = new BigNumber(value);
      this._amount = this._amount.decimalPlaces(6, BigNumber.ROUND_DOWN);
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
      "Target address: " + this._walletAddress + "<br/>" +
      "GOLD amount: " + this._amount + " GOLD<br/>"
      ;
    this._messageBox.confirm(confText).subscribe(ok => {
      if (ok) {
        this._ethService.transferGoldToWallet(ethAddress, this._walletAddress, this._amount);
        this._walletAddressRaw = "";
        this._amount = new BigNumber(0);
      }
      this._cdRef.detectChanges();
    });
  }
}
