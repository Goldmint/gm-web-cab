import { Injectable, OnDestroy } from '@angular/core';
import { BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { BsModalRef } from 'ngx-bootstrap/modal/bs-modal-ref.service';
import { Subscription } from 'rxjs/Subscription';
import { Subject } from 'rxjs/Subject';

import { MessageType } from '../common/message-box/message-box.enum';
import { MessageBoxComponent } from '../common/message-box/message-box.component';

@Injectable()
export class MessageBoxService implements OnDestroy {
  // private static _instance: MessageBoxService;

  private _bsModalRef: BsModalRef;
  private _config: ModalOptions;
  // private _onHidden: Subscription;
  // private _onShown: Subscription;

  static singleMessageId: number;
  static queue: BsModalRef[] = [];

  constructor(private _modalService: BsModalService) {
    this._config = {
      animated: true,
      keyboard: false,
      backdrop: true,
      ignoreBackdropClick: true,
      class: 'message-box'
    };
  }

  ngOnDestroy () {
  }

  // public static get instance() {
  //   if (this._instance === null || this._instance === undefined) {
  //     this._instance = new MessageBoxService(/* */);
  //   }

  //   return this._instance;
  // }

  private _show(message: string, title: string = 'GoldMint.io', clearQueue: boolean = false,
               messageType: MessageType = MessageType.Alert): any {

    let messageBox = new BsModalRef();
        messageBox.content = {
          id:          Date.now(),
          title:       title,
          message:     message,
          messageType: messageType,
          single:      clearQueue,
          onClose:     new Subject(),
          callback:    (content) => {
            let index = MessageBoxService.queue.findIndex(modalRef => modalRef.content.id === content.id);
            MessageBoxService.queue.splice(index, 1);

            if (content.single) {
              delete MessageBoxService.singleMessageId;
            }
            else {
              if (MessageBoxService.queue.length) {
                let nextMessageBox  = MessageBoxService.queue[0];

                let modalRef = this._modalService.show(MessageBoxComponent, this._config);
                    modalRef.content = Object.assign(modalRef.content, nextMessageBox.content);
              }
            }
          }
        };

    if (clearQueue) {
      MessageBoxService.singleMessageId = messageBox.content.id;

      for (let i = 0; i < MessageBoxService.queue.length; ++i) {
        MessageBoxService.queue[i].hide();
        MessageBoxService.queue.splice(i, 1);
      }
    }

    if (!MessageBoxService.queue.length || clearQueue) {
      let modalRef = this._modalService.show(MessageBoxComponent, this._config);
          modalRef.content = Object.assign(modalRef.content, messageBox.content);

      MessageBoxService.queue.push(modalRef);
    }
    else if (!MessageBoxService.singleMessageId) {
      MessageBoxService.queue.push(messageBox);
    }
    else {
      messageBox.content.onClose.complete();
    }

    return messageBox.content.onClose.asObservable();
  }

  public alert(message: string, title?: string, single?: boolean): Subject<any> {
    return this._show(message, title, single);
  }

  public confirm(message: string, title?: string, single?: boolean): Subject<boolean> {
    return this._show(message, title, single, MessageType.Confirm);
  }

  public prompt(message: string, title?: string, single?: boolean): Subject<null|string> {
    return this._show(message, title, single, MessageType.Prompt);
  }

}
