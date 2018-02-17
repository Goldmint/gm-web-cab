export class KYCProfile {
  hasVerificationL0 ?: boolean = false;
  hasVerificationL1 ?: boolean = false;
  firstName   : string = '';
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
