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
  GoldBuyResponse, GoldSellResponse
} from '../interfaces';
import {
  APIResponse, APIPagedResponse, AuthResponse, RegistrationResponse, CardAddResponse,
  CardConfirmResponse, CardStatusResponse, UserBalanceResponse, SwiftInvoice
} from '../interfaces/api-response';

import { KYCProfile } from '../models/kyc-profile';
// import { MessageBoxService as MessageBox } from './message-box.service';
import { environment } from '../../environments/environment';


@Injectable()
export class APIService {
  private _baseUrl = environment.apiUrl;

  constructor(
    private _http: HttpClient,
    /*private _router: Router,*/) {
    console.log('APIService constructor');
  }

  userLogin(username: string, password: string, captcha: string): Observable<APIResponse<AuthResponse>> {
    console.log('APIService userLogin', arguments);

    return this._http.post<APIResponse<AuthResponse>>(`${this._baseUrl}/user/authenticate`, { username: username, password: password, captcha: captcha })
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
      .get(`${this._baseUrl}/user/refresh`, this.jwt())
      .catch(this._handleError)
      .map(x => x.data.token)
      ;
  }

  userLogout() {
    return this._http
      .get(`${this._baseUrl}/user/signout`, this.jwt())
      .retry(3)
      .pipe(
      catchError(this._handleError),
      shareReplay()
      );
  }

  userRegister(email: string, password: string, captcha: string): Observable<APIResponse<RegistrationResponse>> {
    console.log('APIService userRegister', arguments);

    return this._http.post<APIResponse<RegistrationResponse>>(`${this._baseUrl}/register/register`, { email: email, password: password, captcha: captcha })
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

  getCountriesBlacklist() {
    return this._http
      .get(`${this._baseUrl}/commons/bannedCountries`)
      .pipe(
        catchError(this._handleError)
      );
  }

  getCountriesLocalList() {
    return this._http.get('../../assets/data/countries.json');
  }

  banCountry(code: string, comment: string) {
    return this._http
      .post(`${this._baseUrl}/dashboard/countries/ban`, {code, comment}, this.jwt())
      .pipe(
        catchError(this._handleError)
      );
  }

  unbanCountry(code: string) {
    return this._http
      .post(`${this._baseUrl}/dashboard/countries/unban`, {code}, this.jwt())
      .pipe(
        catchError(this._handleError)
      );
  }

  getUsersList(filter: string, offset: number = 0, limit: number = 5,
                  sort: string = 'id', ascending: 'asc' | 'desc' = 'desc'): Observable<APIResponse<TransparencyRecord[]>> {
    let params = {filter, offset, limit, sort, ascending: ascending === 'asc'};

    let httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      })
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/users/list`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  getOplog(id: number, filter: string, offset: number = 0, limit: number = 5,
               sort: string = 'id', ascending: 'asc' | 'desc' = 'desc'): Observable<APIResponse<TransparencyRecord[]>> {
    let params = {id, filter, offset, limit, sort, ascending: ascending === 'asc'};

    let httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      })
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/users/oplog`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  getUsersAccountInfo(id): Observable<APIResponse<TransparencyRecord[]>> {
    let params = {id};

    let httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      })
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/users/account`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );

  }

  setUserAccessRight(id: number, mask): Observable<APIResponse<TransparencyRecord[]>> {

    let httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      })
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/users/rights`, {id, mask}, httpOptions)
      .pipe(
        catchError(this._handleError)
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

  getGoldRate(): Observable<APIResponse<GoldRate>> {
    return this._http
      .get(`${this._baseUrl}/commons/goldRate`)
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

  addIPFSFile(formData) {
    return this._http.post('https://ipfs.infura.io:5001/api/v0/add', formData)
      .pipe(
        catchError(this._handleError)
      );
  }

  getTransparency(offset: number = 0, limit: number = 5,
    sort: string = 'date', ascending: 'asc' | 'desc' = 'desc'): Observable<APIResponse<TransparencyRecord[]>> {
    let params = {
      offset,
      limit,
      sort,
      ascending: ascending === 'asc'
    };

    let httpOptions = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
      })
    };

    return this._http
      .post(`${this._baseUrl}/commons/transparency`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  addTransparency(hash: string, amount: number, comment: string) {
    return this._http
      .post(`${this._baseUrl}/dashboard/transparency/add`, {hash, amount, comment}, this.jwt())
      .pipe(
        catchError(this._handleError)
      );
  }

  getHistory(offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc'): Observable<APIPagedResponse<HistoryRecord[]>> {

    return this._http
      .post(`${this._baseUrl}/user/fiat/history`, { offset: offset, limit: limit, sort: sort, ascending: order === 'asc' }, this.jwt())
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
      .get(`${this._baseUrl}/user/fiat/card/list`, this.jwt())
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
      .post(`${this._baseUrl}/user/fiat/card/add`, { redirect: redirect }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService addFiatCard response', response);

        return response;
      })
      );
  }

  getFiatCardStatus(cardId: number): Observable<APIResponse<CardStatusResponse>> {
    return this._http
      .post(`${this._baseUrl}/user/fiat/card/status`, { cardId: cardId }, this.jwt())
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
      .post(`${this._baseUrl}/user/fiat/card/confirm`, { cardId: cardId, redirect: redirect }, this.jwt())
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
      .post(`${this._baseUrl}/user/fiat/card/verify`, { cardId: cardId, code: code }, this.jwt())
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

  cardWithdraw(cardId: number, amount: number): Observable<APIResponse<any>> {
    return this._http
      .post(`${this._baseUrl}/user/fiat/card/withdraw`, { cardId: cardId, amount: amount }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        console.log('APIService cardWithdraw response', response);
        return response;
      })
      );
  }

  goldBuyReqest(ethAddress: string, amountFiat: number): Observable<APIResponse<GoldBuyResponse>> {
    return this._http
      .post(`${this._baseUrl}/gold/buyRequest`, { ethAddress: ethAddress, amount: amountFiat }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
    );
  }

  goldSellReqest(ethAddress: string, amountGold: BigNumber): Observable<APIResponse<GoldSellResponse>> {
    var wei = new BigNumber(amountGold).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
    return this._http
      .post(`${this._baseUrl}/gold/sellRequest`, { ethAddress: ethAddress, amount: wei.toString() }, this.jwt())
      .pipe(
      catchError(this._handleError),
      shareReplay(),
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

  verifyTFACode(code: number, enable: boolean): Observable<APIResponse<TFAInfo>> {
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

  exchangeTFAToken(code: number): Observable<APIResponse<AuthResponse>> {
    return this._http
      .post(`${this._baseUrl}/user/tfa`, { code: code }, this.jwt())
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
        console.log(res);
        console.log(res.data);
        return res;
      })
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
