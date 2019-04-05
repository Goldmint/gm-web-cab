import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { catchError, tap, shareReplay } from 'rxjs/operators';
import 'rxjs/add/observable/fromEvent';
import 'rxjs/add/observable/merge';
import 'rxjs/add/operator/retry';

import {
  User, HistoryRecord, ActivityRecord, OAuthRedirectResponse,
  TFAInfo, KYCStart,TransparencyRecord, FiatLimits,
  CardsList} from '../interfaces';
import {
  APIResponse, APIPagedResponse, AuthResponse, RegistrationResponse, CardAddResponse,
  CardConfirmResponse, CardStatusResponse
} from '../interfaces/api-response';

import { KYCProfile } from '../models/kyc-profile';
import { environment } from '../../environments/environment';
import {Subject} from "rxjs/Subject";


@Injectable()
export class APIService {

  private _baseUrl = environment.apiUrl;
  private _walletBaseUrl = environment.walletApiUrl;
  private _sumusBaseUrl = environment.sumusNetworkUrl;
  private _marketBaseUrl = environment.marketApiUrl;

  public transferTradingError$ = new Subject();
  public transferTradingLimit$ = new Subject();
  public transferCurrentSumusNetwork = new Subject();

  constructor(private _http: HttpClient) {
    // this.transferCurrentSumusNetwork.subscribe((network: any) => {
    //   this._sumusBaseUrl = environment.sumusNetworkUrl[network];
    // });
  }

  userLogin(username: string, password: string, captcha: string): Observable<APIResponse<AuthResponse>> {
    return this._http.post<APIResponse<AuthResponse>>(`${this._baseUrl}/auth/authenticate`, { username: username, password: password, captcha: captcha, audience: "app" })
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
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
    return this._http.post<APIResponse<RegistrationResponse>>(`${this._baseUrl}/register/register`, { email: email, password: password, captcha: captcha, agreed: agreed })
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
        return response;
      })
      );
  }

  userConfirmEmail(token: string): Observable<APIResponse<any>> {
    return this._http.post<APIResponse<RegistrationResponse>>(`${this._baseUrl}/register/confirm`, { token: token })
      .pipe(
      catchError(this._handleError),
      shareReplay(),
      tap(response => {
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

  getGoldRate(): Observable<object> {
    return this._http
      .get('https://service.goldmint.io/info/rate/v1/gold')
      .pipe(
        catchError(this._handleError),
        shareReplay()
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
        return response;
      })
      );
  }

  goldBuyAsset(ethAddress: string, amount: string, reversed: boolean, currency: string, promoCode: string) {
    return this._http
      .post(`${this._baseUrl}/user/gold/buy/asset/eth`, { ethAddress, amount, reversed, currency, promoCode }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  goldBuyConfirm(requestId: number, promoCode: string) {
    return this._http
      .post(`${this._baseUrl}/user/gold/buy/confirm`, { requestId, promoCode }, this.jwt())
      .pipe(
        catchError(this._handleError),
        shareReplay()
      );
  }

  goldBuyEstimate(currency: string, amount: string, reversed: boolean, promoCode: string) {
    return this._http
      .post(`${this._baseUrl}/user/gold/buy/estimate`, { currency, amount, reversed, promoCode }, this.jwt())
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

  buyGoldFiat(cardId: number, ethAddress: string, currency: string, amount: string, reversed: boolean, promoCode: string) {
    const params = {cardId, ethAddress, currency, amount, reversed, promoCode}

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

  updateKYCProfile(kycProfile: KYCProfile): Observable<APIResponse<KYCProfile>> {
    let profile: any = kycProfile;

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

  getMigrationStatus() {
    return this._http.get(`${this._baseUrl}/user/migration/status`, this.jwt());
  }

  goldMigrationSumus(sumusAddress: string, ethereumAddress: string) {
    return this._http.post(`${this._baseUrl}/user/migration/gold/ethereum`, {sumusAddress, ethereumAddress}, this.jwt());
  }

  goldMigrationEth(sumusAddress: string, ethereumAddress: string) {
    return this._http.post(`${this._baseUrl}/user/migration/gold/sumus`, {ethereumAddress, sumusAddress}, this.jwt());
  }

  mintMigrationSumus(sumusAddress: string, ethereumAddress: string) {
    return this._http.post(`${this._baseUrl}/user/migration/mint/ethereum`, {sumusAddress, ethereumAddress}, this.jwt());
  }

  mintMigrationEth(sumusAddress: string, ethereumAddress: string) {
    return this._http.post(`${this._baseUrl}/user/migration/mint/sumus`, {ethereumAddress, sumusAddress}, this.jwt());
  }

  // pawnshop

  getOrganizationList(from: number) {
    let _from = from !== null ? from : '-'
    return this._http.get(`${this._marketBaseUrl}/org/list/${_from}`);
  }

  getOrganizationDetails(id: number) {
    return this._http.get(`${this._marketBaseUrl}/org/details/${id}`);
  }

  getPawnshopList(org: number, from: number) {
    let _org = org !== null ? org : '-';
    let _from = from !== null ? from : '-';
    return this._http.get(`${this._marketBaseUrl}/pawnshop/list/${_org}/${_from}`);
  }

  getPawnList(pawnshop: number, from: number) {
    let _pawnshop = pawnshop !== null ? pawnshop : '-';
    let _from = from !== null ? from : '-';
    return this._http.get(`${this._marketBaseUrl}/pawn/list/${_pawnshop}/${_from}`);
  }

  getPawnshopDetails(id: number) {
    return this._http.get(`${this._marketBaseUrl}/pawnshop/${id}`);
  }

  getOrganizationsName() {
    return this._http.get(`${this._marketBaseUrl}/org/names`);
  }

  // --------

  // scanner methods

  getScannerStatus() {
    return this._http.get(`${this._sumusBaseUrl}/status`);
  }

  getScannerDailyStatistic() {
    return this._http.get(`${this._sumusBaseUrl}/status/daily`);
  }

  getWalletBalance(sumusAddress: string) {
    return this._http.get(`${this._sumusBaseUrl}/wallet/${sumusAddress}`);
  }

  checkTransactionStatus(digest: string) {
    return this._http.get(`${this._sumusBaseUrl}/tx/${digest}`);
  }

  getTransactionsInBlock(blockNumber: number) {
    return this._http.get(`${this._sumusBaseUrl}/block/${blockNumber}`);
  }

  getScannerBlockList(from: number) {
    let _from: number|string = from;
    if (from == null) _from = "-";
    return this._http.get(`${this._sumusBaseUrl}/block/list/${_from}`);
  }

  getScannerTxList(block: number, address: string, from: string) {
    let _block: number|string = block;
    if (block == null) _block = "-";
    if (address == null) address = "-";
    if (from == null) from = "-";
    return this._http.get(`${this._sumusBaseUrl}/tx/list/${_block}/${address}/${from}`);
  }

  // ---//

  // checkTransactionStatus(hash: string) {
  //   return this._http.post(`${this._walletBaseUrl}/explorer/transaction`, {hash});
  // }

  getNodesCount() {
    return this._http.get(`${this._walletBaseUrl}/statistics/nodes/nodes_count`);
  }

  getMNTCount() {
    return this._http.get(`${this._walletBaseUrl}/statistics/tokens/mnt`);
  }

  getMNTRewardDay(count: number) {
    return this._http.post(`${this._walletBaseUrl}/statistics/tokens/reward`, count);
  }

  getTxDay() {
    return this._http.get(`${this._walletBaseUrl}/statistics/transactions/tx_day`);
  }

  getTransactions(number: number) {
    return this._http.post(`${this._walletBaseUrl}/statistics/transactions/last_tx`, number);
  }

  getBlocks(number: number) {
    return this._http.post(`${this._walletBaseUrl}/statistics/blocks/last_blocks`, number);
  }

  getTxByAddress(sumusAddress: string, offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc') {
    return this._http.post(`${this._walletBaseUrl}/statistics/transactions/tx_by_address`, { sumusAddress, offset, limit, sort, ascending: order === 'asc' });
  }

  // getWalletBalance(sumusAddress: string) {
  //   return this._http.post(`${this._walletBaseUrl}/statistics/tokens/wallet_balance`, { sumusAddress });
  // }

  getAllBlocks(offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc') {
    return this._http.post(`${this._walletBaseUrl}/statistics/blocks/blocks_by_page`, { offset, limit, sort, ascending: order === 'asc' });
  }

  getAllTransactions(offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc') {
    return this._http.post(`${this._walletBaseUrl}/statistics/transactions/tx_by_page`, { offset, limit, sort, ascending: order === 'asc' });
  }

  getActiveNodes(offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc') {
    return this._http.post(`${this._walletBaseUrl}/statistics/nodes/active_nodes`, { offset, limit, sort, ascending: order === 'asc' });
  }

  // getTransactionsInBlock(blockNumber: number, offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc') {
  //   return this._http.post(`${this._walletBaseUrl}/statistics/transactions/tx_from_block`, {blockNumber,  offset, limit, sort, ascending: order === 'asc' });
  // }

  // getRewardTransactions(offset: number = 0, limit: number = null, sort: string = 'date', order: 'asc' | 'desc' = 'desc') {
  //   return this._http.post(`${this._walletBaseUrl}/statistics/transactions/reward`, { offset, limit, sort, ascending: order === 'asc' });
  // }

  // getTotalGoldReward() {
  //   return this._http.get(`${this._walletBaseUrl}/statistics/tokens/total_gold_reward`);
  // }

  // ------

  // master node

  getCurrentActiveNodesStats() {
    return this._http.get(`${this._sumusBaseUrl}/node/stats`);
  }

  getCurrentActiveNodesList(from: string) {
    return this._http.get(`${this._sumusBaseUrl}/node/list/${from || '-'}`);
  }

  getLatestRewardList(from: number) {
    return this._http.get(`${this._sumusBaseUrl}/reward/list/${from || '-'}`);
  }

  getRewardTransactions(id: number, from: number) {
    return this._http.get(`${this._sumusBaseUrl}/reward/${id}/list/${from || '-'}`);
  }

  // -----------

  getUserAccount() {
    return this._http.get(this._baseUrl + '/user/account', this.jwt());
  }

  // ----------

  private _handleError(err: HttpErrorResponse | any) {
    if (err.error && err.error.errorCode) {
      switch (err.error.errorCode) {
        default:
          break;
      }
    }
    else {
      if (!err.message) {
        err.message = 'Unable to retrieve data';
      }
    }

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
