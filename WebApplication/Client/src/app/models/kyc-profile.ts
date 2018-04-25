export class KYCProfile {
  isFormFilled?: boolean = false;
  isKycPending?: boolean = false;
  isKycFinished?: boolean = false;
  isResidencePending?: boolean = false;
  isResidenceProved?: boolean = false;
  isAgreementPending?: boolean = false;
  isAgreementSigned?: boolean = false;

  firstName: string = '';
  middleName ?: string;
  lastName    : string = '';
  /**
   * Date of birth
   * @type {Date}
   * @type {string}
   * @example 'dd.mm.yyyy'
   */
  dob         : Date|null = null;
  phoneNumber : string = '';
  country     : string = '';
  state       : string = '';
  city        : string = '';
  postalCode  : string = '';
  street      : string = '';
  apartment  ?: string;
}
