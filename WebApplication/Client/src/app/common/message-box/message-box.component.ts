import {ChangeDetectorRef, Component} from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { Subject } from 'rxjs/Subject';

import { MessageType } from './message-box.enum';

@Component({
  selector: 'modal-content',
  templateUrl: './message-box.component.html'
})
export class MessageBoxComponent {
  public types = MessageType;
  public onClose: Subject<boolean|string|null>;
  public _promptValue: string;

  public id: string;
  public title: string;
  public message: string;
  public messageType: MessageType;
  public single: boolean;
  public callback: (content) => void;

  constructor(
    private _bsModalRef: BsModalRef,
    private _cdRef: ChangeDetectorRef
  ) {
    this.onClose = new Subject();
    this.single = false;
    this.callback = (content) => {};
    setTimeout(() => this._cdRef.detectChanges(), 0);
  }

  public onConfirm(): void {
    switch (this.messageType) {
      case MessageType.Prompt:
        this.onClose.next(this._promptValue && this._promptValue.length ? this._promptValue : null);
        break;

      case MessageType.Confirm:
      default:
        this.onClose.next(true);
        break;
    }

    this.onClose.complete();
    this.hide();
  }

  public onCancel(): void {
    this.onClose.complete();
    this.hide();
  }

  public hide() {
    this.callback(this);
    this._bsModalRef.hide();
  }
}
