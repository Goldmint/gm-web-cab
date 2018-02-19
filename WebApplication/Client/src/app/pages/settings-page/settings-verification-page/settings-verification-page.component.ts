import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
// import { PhoneNumberComponent } from 'ngx-international-phone-number';
import 'rxjs/add/operator/finally';

import { APIResponse, Country, Region, KYCStart } from '../../../interfaces';
import { APIService, MessageBoxService } from '../../../services';
import { KYCProfile } from '../../../models/kyc-profile';

import * as countries from '../../../../assets/data/countries.json';

enum Phase {Start, Basic, Finished}

@Component({
  selector: 'app-settings-verification-page',
  templateUrl: './settings-verification-page.component.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsVerificationPageComponent implements OnInit {
  // @ViewChild("pho", {read: PhoneNumberComponent}) pho: PhoneNumberComponent;

  public _phase = Phase;

  public loading = true;
  public processing = false;
  public repeat = Array;
  public buttonBlur = new EventEmitter<boolean>();

  public phase: Phase;
  public countries: Country[];
  public regions: Region[];
  public errors = [];

  public kycProfile = new KYCProfile();
  public dateOfBirth: { day: number, month: number, year: number|'' };
  public minBirthYear = 1999;

  constructor(
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService) {

    this.dateOfBirth = { day: 1, month: 1, year: '' };
    this.countries = <Country[]><any> countries;

    this.phase = Phase.Start;

    this._apiService.getKYCProfile()
      .finally(() => {
        this.loading = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        (res: APIResponse<KYCProfile>) => {
          this.kycProfile = res.data;

          if (this.kycProfile.dob) {
            this.dateOfBirth = {
              day:   this.kycProfile.dob.getDate(),
              month: this.kycProfile.dob.getMonth() + 1,
              year:  this.kycProfile.dob.getFullYear()
            };
          }

          this.onCountrySelect(false);

          this.phase = this.kycProfile.hasVerificationL1
            ? Phase.Finished
            : this.kycProfile.hasVerificationL0
              ? Phase.Basic
              : Phase.Start;
        },
        err => {});
  }

  ngOnInit() {
  }

  setVerificationPhase(phase: Phase) {
    this.buttonBlur.emit();
    this.phase = phase;
  }

  onCountrySelect(reset: boolean = true) {
    const country = <Country> this.countries.find(country => country.countryShortCode === this.kycProfile.country);

    if (reset) this.kycProfile.state = null;

    if (country != null) {
      this.regions = country.regions;
    }
  }

  submit(kycForm?: NgForm) {
    if (this.kycProfile.hasVerificationL0 === true) {
      this.startKYCVerification();
    }
    else if (kycForm) {
      this.submitBasicInformation(kycForm);
    }
  }

  submitBasicInformation(kycForm: NgForm) {
    this.processing = true;

    this.kycProfile.dob = new Date(<number>this.dateOfBirth.year, this.dateOfBirth.month - 1, this.dateOfBirth.day);

    this._apiService.updateKYCProfile(this.kycProfile)
      .finally(() => {
        kycForm.form.markAsPristine();

        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        (res: APIResponse<KYCProfile>) => {
          this.kycProfile = res.data;
        },
        err => {
          if (err.error.errorCode) {
            switch (err.error.errorCode) {
              case 100: // InvalidParameter
                for (let i = err.error.data.length - 1; i >= 0; i--) {
                  this.errors[err.error.data[i].field] = err.error.data[i].desc;
                }
                break;

              default:
                this._messageBox.alert(err.error.errorDesk);
                break;
            }
          }
        });
  }

  startKYCVerification() {
    this.processing = true;

    this._apiService.startKYCVerification(window.location.href)
      .finally(() => {
        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        (res: APIResponse<KYCStart>) => {
          this._messageBox.confirm('You\'ll be redirected to the verification service.')
            .subscribe(confirmed => {
              if (confirmed) {
                localStorage.setItem('gmint_kycTicket', String(res.data.ticketId));
                window.location.href = res.data.redirect;
              }
            });
        },
        err => {});
  }

}
