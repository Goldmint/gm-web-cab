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
  public walletAddressVal: string = null;
  public walletAddress: string = null;

  amountUnset: boolean = true;
  goldBalance:BigNumber = null;

  walletChecked:boolean = true;
  amountChecked: boolean = true;

  constructor(
    private _modalService: BsModalService,
    private _ethService: EthereumService,
    private _messageBox: MessageBoxService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this._ethService.getObservableGoldBalance()
      .subscribe(val => {
        this.goldBalance = val;
        this.validateAmount();
        this._cdRef.markForCheck();
      });
  }

  modal(template: TemplateRef<any>) {
    if (this._modalRef) {
      this._modalRef.hide();
    }
    this._modalRef = this._modalService.show(template, { class: 'modal-lg' });
  }

  onWalletAddressChanged(value: string) {
    this.walletAddress = null;
    this.walletChecked = false;

    if (this._ethService.isValidAddress(value)) {
      this.walletAddress = value;
      this.walletChecked = true;
    }
    this._cdRef.markForCheck();
  }

  onAmountChanged(value: string) {
    this.amountUnset = false;
    this.amount = new BigNumber(0);

    var testVal = value != null && value.length > 0 ? parseFloat(value) : 0;
    if (testVal > 0) {
      this.amount = new BigNumber(value);
      this.amount = this.amount.decimalPlaces(6, BigNumber.ROUND_DOWN);
    }
    this.validateAmount();
    this._cdRef.markForCheck();
  }

  validateAmount() {
    this.amountChecked = this.amountUnset || this.amount.gt(0) && this.goldBalance && this.amount.lte(this.goldBalance);
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
        this.walletAddressVal = "";
        this.amount = new BigNumber(0);
      }
      this._cdRef.markForCheck();
    });
  }
}
