import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs/Observable';
import { ErrorObservable } from 'rxjs/observable/ErrorObservable';
import { catchError, tap, shareReplay } from 'rxjs/operators';
import 'rxjs/add/observable/fromEvent';
import 'rxjs/add/observable/merge';
import 'rxjs/add/operator/retry';

import {User, TFAInfo, TransparencyRecord} from '../interfaces';
import {APIResponse, AuthResponse} from '../interfaces/api-response';
import { environment } from '../../environments/environment';


@Injectable()
export class APIService {
  private _baseUrl = environment.apiUrl;

  constructor(private _http: HttpClient) {
    console.log('APIService constructor');
  }

  userLogin(username: string, password: string, captcha: string): Observable<APIResponse<AuthResponse>> {
    console.log('APIService userLogin', arguments);

    return this._http.post<APIResponse<AuthResponse>>(`${this._baseUrl}/auth/authenticate`, { username: username, password: password, captcha: captcha, audience: "dashboard" })
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

  getBannedCountries(offset: number = 0, limit: number = 5,
                     sort: string = 'date', ascending: 'asc' | 'desc' = 'desc'): Observable<APIResponse<TransparencyRecord[]>> {
    let params = {offset, limit, sort, ascending: ascending === 'asc'};

    let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/countries/list`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  };

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
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/users/list`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  setProvedResidence(id: number, link: string) {

    let params = {id: id, proved: true, link: link};

    let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/users/proveResidence`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  getOplog(id: number, filter: string, offset: number = 0, limit: number = 5,
               sort: string = 'id', ascending: 'asc' | 'desc' = 'desc'): Observable<APIResponse<TransparencyRecord[]>> {
    let params = {id, filter, offset, limit, sort, ascending: ascending === 'asc'};

   let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
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
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/users/account`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );

  }

  setUserAccessRight(id: number, mask): Observable<APIResponse<TransparencyRecord[]>> {

   let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
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
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/commons/transparency`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  updateStatTransparency(data) {
    return this._http
      .post(`${this._baseUrl}/dashboard/transparency/updateStat`, data, this.jwt())
      .pipe(
        catchError(this._handleError)
      );
  }

  addTransparency(link: string, amount: string, comment: string) {
    return this._http
      .post(`${this._baseUrl}/dashboard/transparency/add`, {link, amount, comment}, this.jwt())
      .pipe(
        catchError(this._handleError)
      );
  }

  getFees() {
    return this._http
      .get(`${this._baseUrl}/commons/fees`)
      .pipe(
        catchError(this._handleError)
      );
  }

  updateFees(data) {
    return this._http
      .post(`${this._baseUrl}/dashboard/fees/update`, data, this.jwt())
      .pipe(
        catchError(this._handleError)
      );
  }

  getSwiftList(filter: string, excludeCompleted: boolean, type: number, offset: number = 0, limit: number = 5,
               sort: string = 'date', ascending: 'asc' | 'desc' = 'desc') {
    let params = {
      filter,
      excludeCompleted,
      type,
      offset,
      limit,
      sort,
      ascending: ascending === 'asc'
    };

    let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/swift/list`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  swiftLockDeposit(id: number) {
    let params = { id };

    let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/swift/lockDeposit`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  swiftLockWithdraw(id: number) {
    let params = { id };

    let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/swift/lockWithdraw`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }


  swiftRefuseDeposit(id: number, comment: string) {
    let params = { id, comment };

    let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/swift/refuseDeposit`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  swiftRefuseWithdraw(id: number, comment: string) {
    let params = { id, comment };

    let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/swift/refuseWithdraw`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
      );
  }

  swiftAcceptDeposit(id: number, amount: number, comment: string) {
    let params = { id, amount, comment };

    let httpOptions = {
      headers: this.jwt().headers.append('Content-Type', 'application/json')
    };

    return this._http
      .post(`${this._baseUrl}/dashboard/swift/acceptDeposit`, params, httpOptions)
      .pipe(
        catchError(this._handleError)
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

  changePassword(currentPassword: string, newPassword: string, tfaCode: string): Observable<APIResponse<any>> {
    return this._http
      .post(`${this._baseUrl}/user/settings/changePassword`, { current: currentPassword, new: newPassword, tfaCode: tfaCode }, this.jwt())
      .pipe(
      catchError(this._handleError)
      );
  }

  private _handleError(err: HttpErrorResponse | any) {
    if (err.error && err.error.errorCode) {
      console.info('API Error', err.error.errorCode, err.error.errorDesc);
    }
    else {
      if (!err.message) {
        err.message = 'Unable to retrieve data';
      }

      console.info('HTTP Error', err.message);
    }

    return ErrorObservable.create(err);
  }

  public jwt(): { headers?: HttpHeaders, params?: HttpParams } {
    let token = localStorage.getItem('gmint_token');

    let result = {
      headers: new HttpHeaders()
    };

    if (token) {
      result.headers = result.headers.append('Authorization', `Bearer ${token}`);
    }

    return result;
  }

}
