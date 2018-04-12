import { Component } from '@angular/core';
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
  public promptValue: string;

  public id: string;
  public title: string;
  public message: string;
  public messageType: MessageType;
  public single: boolean;
  public callback: (content) => void;

  constructor(private _bsModalRef: BsModalRef) {
    this.onClose = new Subject();
    this.single = false;
    this.callback = (content) => {};
  }

  public onConfirm(): void {
    switch (this.messageType) {
      case MessageType.Prompt:
        this.onClose.next(this.promptValue && this.promptValue.length ? this.promptValue : null);
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

  // get observable() {
  //   this._onClose.asObservable();
  // }

}
