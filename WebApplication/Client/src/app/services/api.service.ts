import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { catchError, tap, shareReplay } from 'rxjs/operators';
import 'rxjs/add/observable/fromEvent';
import 'rxjs/add/observable/merge';
import 'rxjs/add/operator/retry';
import { BigNumber } from 'bignumber.js'

import {
  User, HistoryRecord, ActivityRecord, OAuthRedirectResponse,
  GoldRate, TFAInfo, KYCStart, KYCStatus, TransparencyRecord, FiatLimits,
  CardsList,
  GoldBuyResponse, GoldSellResponse, KYCAgreementResend
} from '../interfaces';
import {
  APIResponse, APIPagedResponse, AuthResponse, RegistrationResponse, CardAddResponse,
  CardConfirmResponse, CardStatusResponse, UserBalanceResponse, SwiftInvoice
} from '../interfaces/api-response';

import { KYCProfile } from '../models/kyc-profile';
// import { MessageBoxService as MessageBox } from './message-box.service';

import { environment } from '../../environments/environment';
import {GoldHwSellResponse} from "../interfaces/api-response/gold-hw-sell";
import {GoldHwBuyResponse} from "../interfaces/api-response/gold-hw-buy";
import {GoldHwTransferResponse} from "../interfaces/api-response/gold-hw-transfer";
import {Subject} from "rxjs/Subject";


@Injectable()
export class APIService {
  private _baseUrl = environment.apiUrl;
  public transferTradingError$ = new Subject();
  public transferTradingLimit$ = new Subject();

  constructor(private _http: HttpClient) {
    console.log('APIService constructor');
  }

  userLogin(username: string, password: string, captcha: string): Observable<APIResponse<AuthResponse>> {
    console.log('APIService userLogin', arguments);

    return this._http.post<APIResponse<AuthResponse>>(`${this._baseUrl}/auth/authenticate`, { username: username, password: password, captcha: captcha, audience: "app" })
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService userLogin response', response);

        return response;
      })
      );
  }

  userRefreshToken(): Observable<string> {
    return this._http
      .get(`${this._baseUrl}/auth/refresh`, this.jwt())
      .catch(this._handleError)
      .map(x => x.data.token)
      ;
  }

  userLogout() {
    return this._http
      .get(`${this._baseUrl}/auth/signout`, this.jwt())
      .retry(3)
      .pipe(
      catchError(this._handleError),
      shareReplay()
      );
  }

  getTradingStatus() {
    return this._http
      .get(`${this._baseUrl}/commons/status`, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  dpaCheck(token: string) {
    return this._http
      .post(`${this._baseUrl}/auth/dpaCheck`, {token}, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  userRegister(email: string, password: string, captcha: string, agreed: boolean): Observable<APIResponse<RegistrationResponse>> {
    console.log('APIService userRegister', arguments);

    return this._http.post<APIResponse<RegistrationResponse>>(`${this._baseUrl}/register/register`, { email: email, password: password, captcha: captcha, agreed: agreed })
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService userRegister response', response);

        return response;
      })
      );
  }

  userConfirmEmail(token: string): Observable<APIResponse<any>> {
    console.log('APIService userConfirmEmail', arguments);

    return this._http.post<APIResponse<RegistrationResponse>>(`${this._baseUrl}/register/confirm`, { token: token })
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService userConfirmEmail response', response);
        return response;
      })
      );
  }

  userRestorePassword(email: string, captcha: string): Observable<APIResponse<any>> {
    return this._http.post<APIResponse<any>>(`${this._baseUrl}/restore/password`, { email: email, captcha: captcha })
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService userRestorePassword response', response);

        return response;
      })
      );
  }

  userChangePassword(token: string, password: string): Observable<APIResponse<any>> {
    return this._http.post<APIResponse<any>>(`${this._baseUrl}/restore/newPassword`, { token: token, password: password })
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService userChangePassword response', response);

        return response;
      })
      );
  }

  getGoogleOAuthUrl(): Observable<APIResponse<OAuthRedirectResponse>> {
    return this._http
      .get(`${this._baseUrl}/oauth/google`)
      .retry(3)
      .pipe(
      catchError(this._handleError),
      shareReplay()
      );
  }

  getProfile(): Observable<APIResponse<User>> {
    return this._http
      .get(`${this._baseUrl}/user/profile`, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay()
      );
  }

  // setLocale(locale: string) {
  //   return this._http
  //     .post(`${this._baseUrl}?action=/api/setLocale`, { locale: locale })
  //     .pipe(
  //       catchError(this._handleError),
  //       shareReplay()
  //     );
  // }

  getGoldRate(): Observable<object> {
    return this._http
      .get('https://service.goldmint.io/info/rate/v1/gold')
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  getUserBalance(): Observable<APIResponse<UserBalanceResponse>> {
    return this._http
      .post(`${this._baseUrl}/user/balance`, {}, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
    );
  }

  getZendeskTokenSSO() {
    return this._http
      .get(`${this._baseUrl}/user/zendesk/sso`, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  getLimits(): Observable<APIResponse<FiatLimits>> {
    return this._http
      .get(`${this._baseUrl}/user/limits`, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService getLimits response', response);

        return response;
      })
      );
  }

  getTransparency(offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc'): Observable<APIResponse<TransparencyRecord[]>> {
    return this._http
      .post(`${this._baseUrl}/commons/transparency`, { offset: offset, limit: limit, sort: sort, ascending: order === 'asc' })
      .pipe(
        catchError(this._handleError)
      );
  }

  getHistory(offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc'): Observable<APIPagedResponse<HistoryRecord[]>> {

    return this._http
      .post(`${this._baseUrl}/user/history`, { offset: offset, limit: limit, sort: sort, ascending: order === 'asc' }, this.jwt())
      .pipe(
      catchError(this._handleError),
      tap(response => {
        console.log('APIService getHistory response', response);

        return response;
      })
      );
  }

  getActivity(offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc'): Observable<APIPagedResponse<ActivityRecord>> {

    return this._http
      .post(`${this._baseUrl}/user/activity`, { offset: offset, limit: limit, sort: sort, ascending: order === 'asc' }, this.jwt())
      .pipe(
      catchError(this._handleError),
      tap(response => {
        console.log('APIService getActivity response', response);

        return response;
      })
      );
  }

  getFiatCards(): Observable<APIResponse<CardsList>> {
    return this._http
      .get(`${this._baseUrl}/user/ccard/list`, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService getFiatCards response', response);

        return response;
      })
      );
  }

  addFiatCard(redirect: string): Observable<APIResponse<CardAddResponse>> {
    return this._http
      .post(`${this._baseUrl}/user/ccard/add`, { redirect: redirect }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService addFiatCard response', response);

        return response;
      })
      );
  }

  removeFiatCard(cardId: number): Observable<APIResponse<CardAddResponse>> {
    return this._http
      .post(`${this._baseUrl}/user/ccard/remove`, { cardId }, this.jwt())
      .pipe(
        catchError(this._handleError)
      );
  }

  getFiatCardStatus(cardId: number): Observable<APIResponse<CardStatusResponse>> {
    return this._http
      .post(`${this._baseUrl}/user/ccard/status`, { cardId: cardId }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService getFiatCardStatus response', response);

        return response;
      })
      );
  }

  confirmFiatCard(cardId: number, redirect: string): Observable<APIResponse<CardConfirmResponse>> {
    return this._http
      .post(`${this._baseUrl}/user/ccard/confirm`, { cardId: cardId, redirect: redirect }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService confirmFiatCard response', response);

        return response;
      })
      );
  }

  verifyFiatCard(cardId: number, code: number | string): Observable<APIResponse<any>> {
    return this._http
      .post(`${this._baseUrl}/user/ccard/verify`, { cardId: cardId, code: code }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService verifyFiatCard response', response);

        return response;
      })
      );
  }

  cardDeposit(cardId: number, amount: number): Observable<APIResponse<any>> {
    return this._http
      .post(`${this._baseUrl}/user/fiat/card/deposit`, { cardId: cardId, amount: amount }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService cardDeposit response', response);
        return response;
      })
      );
  }

  cardWithdraw(cardId: number, amount: number, code: string): Observable<APIResponse<any>> {
    return this._http
      .post(`${this._baseUrl}/user/fiat/card/withdraw`, { cardId, amount, code }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService cardWithdraw response', response);
        return response;
      })
      );
  }

  goldBuyRequest(ethAddress: string, amountFiat: number): Observable<APIResponse<GoldBuyResponse>> {
    return this._http
      .post(`${this._baseUrl}/user/exchange/gold/buy`, { ethAddress: ethAddress, amount: amountFiat }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
    );
  }

  goldSellRequest(ethAddress: string, amountGold: BigNumber): Observable<APIResponse<GoldSellResponse>> {
    var wei = new BigNumber(amountGold).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
    return this._http
      .post(`${this._baseUrl}/user/exchange/gold/sell`, { ethAddress: ethAddress, amount: wei.toString() }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
    );
  }

  goldSellHwRequest(amount: BigNumber): Observable<APIResponse<GoldHwSellResponse>> {
    var wei = new BigNumber(amount).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
    return this._http
      .post(`${this._baseUrl}/user/exchange/gold/hw/sell`, { amount: wei.toString() }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  goldBuyHwRequest(amount: number): Observable<APIResponse<GoldHwBuyResponse>> {
    return this._http
      .post(`${this._baseUrl}/user/exchange/gold/hw/buy`, { amount }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }
  // -------
  goldBuyAsset(ethAddress: string, amount: string, reversed: boolean, currency: string) {
    return this._http
      .post(`${this._baseUrl}/user/gold/buy/asset/eth`, { ethAddress, amount, reversed, currency }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  goldBuyConfirm(requestId: number) {
    return this._http
      .post(`${this._baseUrl}/user/gold/buy/confirm`, { requestId }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  goldBuyEstimate(currency: string, amount: string, reversed: boolean) {
    return this._http
      .post(`${this._baseUrl}/user/gold/buy/estimate`, { currency, amount, reversed }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  goldSellAsset(ethAddress: string, amount: string, reversed: boolean, currency: string) {
    return this._http
      .post(`${this._baseUrl}/user/gold/sell/asset/eth`, { ethAddress, amount, reversed, currency }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  goldSellConfirm(requestId: number) {
    return this._http
      .post(`${this._baseUrl}/user/gold/sell/confirm`, { requestId }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  goldSellEstimate(ethAddress: string, currency: string, amount: string, reversed: boolean) {
    return this._http
      .post(`${this._baseUrl}/user/gold/sell/estimate`, { ethAddress, currency, amount, reversed }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  goldTransferHwRequest(ethAddress: string, amount: BigNumber): Observable<APIResponse<GoldHwTransferResponse>> {
    var wei = new BigNumber(amount).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
    return this._http
      .post(`${this._baseUrl}/user/exchange/gold/hw/transfer`, { ethAddress, amount: wei.toString() }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  buyGoldFiat(cardId: number, ethAddress: string, currency: string, amount: string, reversed: boolean) {
    const params = {cardId, ethAddress, currency, amount, reversed}

    return this._http
      .post(`${this._baseUrl}/user/gold/buy/ccard`, params, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  sellGoldFiat(cardId: number, ethAddress: string, currency: string, amount: string, reversed: boolean) {
    const params = {cardId, ethAddress, currency, amount, reversed}

    return this._http
      .post(`${this._baseUrl}/user/gold/sell/ccard`, params, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  confirmHwRequest(isBuying: boolean, requestId: number) {
    return this._http
      .post(`${this._baseUrl}/user/exchange/gold/hw/confirm`, { isBuying, requestId }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  confirmMMRequest(isBuying: boolean, requestId: number) {
    return this._http
      .post(`${this._baseUrl}/user/exchange/gold/confirm`, { isBuying, requestId }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  ethDepositRequest(ethAddress: string, amountCoin: BigNumber) {
    var wei = new BigNumber(amountCoin).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
    return this._http
      .post(`${this._baseUrl}/user/fiat/asset/depositEth`, { ethAddress: ethAddress, amount: wei.toString() }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  confirmEthDepositRequest(isDeposit: boolean, requestId: number) {
    return this._http
      .post(`${this._baseUrl}/user/fiat/asset/confirm`, { isDeposit, requestId }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  getEthereumRate() {
    return this._http
      .get(`${this._baseUrl}/commons/ethRate`)
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  getBannedCountries() {
    return this._http
      .get(`${this._baseUrl}/commons/bannedCountries`)
      .pipe(
        catchError(this._handleError)
      );
  };

  testPassword(pass) {
    return this._http
      .get(`https://api.pwnedpasswords.com/pwnedpassword/${pass}`)
  }

  getFees() {
    return this._http
      .get(`${this._baseUrl}/commons/fees`)
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  getTFAInfo(): Observable<APIResponse<TFAInfo>> {
    return this._http
      .get(`${this._baseUrl}/user/settings/tfa/view`, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService getTFAInfo response', response);

        return response;
      })
      );
  }

  verifyTFACode(code: string, enable: boolean): Observable<APIResponse<TFAInfo>> {
    return this._http
      .post(`${this._baseUrl}/user/settings/tfa/edit`, { enable: enable, code: code }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService verifyTFACode response', response);

        return response;
      })
      );
  }

  exchangeTFAToken(code: string): Observable<APIResponse<AuthResponse>> {
    return this._http
      .post(`${this._baseUrl}/auth/tfa`, { code: code }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService exchangeTFAToken response', response);

        return response;
      })
      );
  }

  getKYCProfile(): Observable<APIResponse<KYCProfile>> {
    return this._http
      .get(`${this._baseUrl}/user/settings/verification/view`, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService getKYCProfile response', response);

        if (response.data.dob && response.data.dob.length) {
          const [day, month, year] = response.data.dob.split('.');

          response.data.dob = new Date(year, month - 1, day);
        }

        return response;
      })
      );
  }

  startKYCVerification(redirect: string): Observable<APIResponse<KYCStart>> {
    return this._http
      .post(`${this._baseUrl}/user/settings/verification/kycStart`, { redirect: redirect }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService startKYCVerification response', response);

        return response;
      })
      );
  }

  agreedWithTos(): Observable<APIResponse<KYCProfile>> {
    return this._http
      .get(`${this._baseUrl}/user/settings/verification/agreedWithTos`, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }

  resendKYCAgreement(): Observable<APIResponse<KYCProfile>> {
    return this._http
      .get(`${this._baseUrl}/user/settings/verification/resendAgreement`, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
      );
  }
  /*
  getKYCVerificationStatus(ticketId: number): Observable<APIResponse<KYCStatus>> {
    return this._http
      .post(`${this._baseUrl}/user/settings/verification/kycStatus`, { ticketId: ticketId }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService getKYCVerificationStatus response', response);

        return response;
      })
      );
  }
  */
  updateKYCProfile(kycProfile: KYCProfile): Observable<APIResponse<KYCProfile>> {
    let profile: any = kycProfile;

    //@todo: maybe replace by moment.js imp-n
    if (kycProfile.dob instanceof Date) {
      let day = String(kycProfile.dob.getDate());
      let month = String(kycProfile.dob.getMonth() + 1);
      const year = String(kycProfile.dob.getFullYear());

      if (day.length < 2) day = '0' + day;
      if (month.length < 2) month = '0' + month;

      profile.dob = `${day}.${month}.${year}`;
    }

    return this._http
      .post(`${this._baseUrl}/user/settings/verification/edit`, profile, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService updateKYCProfile response', response);

        if (response.data.dob && response.data.dob.length) {
          const [day, month, year] = response.data.dob.split('.');

          response.data.dob = new Date(year, month - 1, day);
        }

        return response;
      })
      );
  }

  changePassword(currentPassword: string, newPassword: string, tfaCode: string): Observable<APIResponse<any>> {
    return this._http
      .post(`${this._baseUrl}/user/settings/changePassword`, { current: currentPassword, new: newPassword, tfaCode: tfaCode }, this.jwt())
      .pipe(
      catchError(this._handleError)
      );
  }

  getSwiftDepositInvoice(amount: number): Observable<APIResponse<SwiftInvoice>> {
    let data = {
      amount: amount,
    };
    let headers = this.jwt();
    console.log(headers);
    return this._http
      .post(`${this._baseUrl}/user/fiat/swift/deposit`, data, headers)
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(res => {
        return res;
      })
      );
  }

  getSwiftWithdrawInvoice(amount: number, templateId: number) {
    let data = { amount, templateId };

    return this._http
      .post(`${this._baseUrl}/user/fiat/swift/withdraw`, data, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay(),
        tap(res => {
          return res;
        })
      );
  }

  getSwiftWithdrawTemplatesList() {
    return this._http
      .get(`${this._baseUrl}/user/fiat/swift/list`, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  addSwiftWithdrawTemplate(data: object) {
    return this._http
      .post(`${this._baseUrl}/user/fiat/swift/add`, data, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  removeSwiftWithdrawTemplate(templateId: number) {
    return this._http
      .post(`${this._baseUrl}/user/fiat/swift/remove`, {templateId}, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  private _handleError(err: HttpErrorResponse | any) {
    // if (err == 'ohO_offline') {
    //   MessageBox.instance.alert('Seems like your internet connection is lost.<br>Please check it and try again.', 'Connection error');
    // }
    // else {
    if (err.error && err.error.errorCode) {
      switch (err.error.errorCode) {
        // case 50: // Unauthorized
        //   this._router.navigate(['/signin']);
        //   alert(err.error.errorDesc);
        //   break;

        default:
          console.info('API Error', err.error.errorCode, err.error.errorDesc);
          break;
      }
    }
    else {
      if (!err.message) {
        err.message = 'Unable to retrieve data';
      }

      console.info('HTTP Error', err.message);
    }

    //   if (err.status === 404) {
    //     MessageBox.instance.alert('GoldMint server does not respond. Please try again in few minutes.', 'Connection error');
    //   }
    // }

    return ErrorObservable.create(err);
  }

  public jwt(): { headers?: HttpHeaders, params?: HttpParams } {
    let token = localStorage.getItem('gmint_token');

    if (token) {
      return {
        headers: new HttpHeaders().set('Authorization', `Bearer ${token}`)
      };
    }

    return {};
  }

  public get_baseUrl(): string {
    return this._baseUrl;
  }

}
