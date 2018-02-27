import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, HostBinding, TemplateRef, ChangeDetectorRef } from '@angular/core';
import { BsModalService } from 'ngx-bootstrap/modal';
import { BsModalRef } from 'ngx-bootstrap/modal/bs-modal-ref.service';
import { MessageBoxService, EthereumService } from "../../services/index";
import { BigNumber } from 'bignumber.js'
import {Observable} from "rxjs/Observable";
import {APIService} from "../../services";

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
  goldBalance: BigNumber = null;
  goldHotBalance: BigNumber = null;
  goldMetamaskBalance: BigNumber = null;

  walletChecked:boolean = true;
  amountChecked: boolean = true;

  public amountValue: number;

  public ethAddress: string = '';
  public selectedWallet = 0;

  constructor(
    private _modalService: BsModalService,
    private _ethService: EthereumService,
    private _messageBox: MessageBoxService,
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    Observable.combineLatest(
      this._ethService.getObservableHotGoldBalance(),
      this._ethService.getObservableGoldBalance(),
      this._ethService.getObservableEthAddress()
    ).subscribe(data => {
      if (this.ethAddress !== data[2]) {
        this.ethAddress = data[2];
        this.selectedWallet = this.ethAddress ? 1 : 0;
      }

      this.goldHotBalance = data[0];
      this.goldMetamaskBalance = data[1];
      this.goldBalance = this.selectedWallet == 0 ? data[0] : data[1];

      this.validateAmount();
      this._cdRef.markForCheck();
    });

  }

  onChangeWallet() {
    this.goldBalance  = this.selectedWallet == 0 ?  this.goldHotBalance : this.goldMetamaskBalance;
    this.validateAmount();
  }

  modal(template: TemplateRef<any>) {
    if (this.selectedWallet == 1) {
      if (this._modalRef) {
        this._modalRef.hide();
      }
      this._modalRef = this._modalService.show(template, { class: 'modal-lg' });
    } else {
      this.onHotWallet();
    }
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
      var confText =
        "Target address: " + this.walletAddress + "<br/>" +
        "GOLD amount: " + this.amount + " GOLD<br/>"
      ;
      this._messageBox.confirm(confText).subscribe(ok => {
        if (ok) {
          this._ethService.transferGoldToWallet(this.ethAddress, this.walletAddress, this.amount);
          this.walletAddressVal = "";
          this.amount = new BigNumber(0);
          this.amountValue = null;
        }
        this._cdRef.markForCheck();
      });
  }

  onHotWallet() {
    var confText =
      "Target address: " + this.walletAddress + "<br/>" +
      "GOLD amount: " + this.amount + " GOLD<br/>";

    this._messageBox.confirm(confText).subscribe(ok => {
      if(ok) {
        this._apiService.goldTransferHwRequest(this.walletAddress, this.amount.toString())
          .subscribe(() => {
            this._messageBox.alert('Your request is in progress now!');
            this.walletAddressVal = "";
            this.amount = new BigNumber(0);
            this.amountValue = null;
          }, err => {
          err.error && err.error.errorCode && this._messageBox.alert(err.error.errorCode == 1010
            ? 'You have exceeded request frequency (One request for 30 minutes). Please try later'
            : err.error.errorDesc)
          })
      }
    });
  }

}
