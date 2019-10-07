import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { Observable } from 'rxjs/Observable';
import { MessageBoxService } from './message-box.service';
import { APIService } from './api.service';
import { AppDefaultLanguage } from '../app.languages';
import {Subject} from "rxjs/Subject";
import {TranslateService} from "@ngx-translate/core";
import {environment} from "../../environments/environment";

@Injectable()
export class UserService {

  private _locale = new BehaviorSubject<string>(AppDefaultLanguage || 'en');

  public currentLocale: Observable<string> = this._locale.asObservable();
  public getLiteWalletLink;
  public windowSize$ = new Subject();

  constructor(
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _translate: TranslateService
  ) {
    let isFirefox = typeof window['InstallTrigger'] !== 'undefined';
    this.getLiteWalletLink = isFirefox ? environment.getLiteWalletLink.firefox : environment.getLiteWalletLink.chrome;
  }

  showLoginToMMBox(heading: string) {
    this._translate.get('MessageBox.LoginToMM').subscribe(phrase => {
      this._messageBox.alert(`
        <div class="text-center">${phrase.Text}</div>
        <div class="metamask-icon"></div>
        <div class="text-center mt-2 mb-2">MetaMask</div>
      `, phrase[heading]);
    });
  }

  showGetMetamaskModal() {
    this._translate.get('MessageBox.MetaMask').subscribe(phrase => {
      this._messageBox.alert(phrase.Text, phrase.Heading);
    });
  }

  showLoginToLiteWalletModal() {
    this._translate.get('MessageBox.LoginToLiteWallet').subscribe(phrase => {
      this._messageBox.alert(`
        <div class="text-center">${phrase.Text}</div>
        <div class="gold-circle-icon"></div>
        <div class="text-center mt-2 mb-2">Lite Wallet</div>
      `, phrase.Heading);
    });
  }

  showGetLiteWalletModal() {
    this._translate.get('MessageBox.LiteWallet').subscribe(phrase => {
      this._messageBox.alert(`
            <div>${phrase.Text} <a href="${this.getLiteWalletLink}" target="_blank">Lite Wallet</a></div>
      `, phrase.Heading);
    });
  }

  invalidNetworkModal(network) {
    this._translate.get('MessageBox.InvalidNetwork', {network}).subscribe(phrase => {
      setTimeout(() => {
        this._messageBox.alert(phrase);
      }, 0);
    });
  }

  showInvalidNetworkModal(translateKey, network) {
    this._translate.get('MessageBox.' + translateKey, {network}).subscribe(phrase => {
      setTimeout(() => {
        this._messageBox.alert(phrase);
      }, 0);
    });
  }

  public setLocale(locale: string) {
    this._locale.next(locale);
  }
}
