import { Directive, forwardRef, Attribute } from '@angular/core';
import { Validator, AbstractControl, NG_VALIDATORS } from '@angular/forms';

@Directive({
  selector: '[equal][formControlName],[equal][formControl],[equal][ngModel]',
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => EqualValidatorDirective), multi: true
    }
  ]
})
export class EqualValidatorDirective implements Validator {

  constructor(
    @Attribute('equal') public equal: string,
    @Attribute('reverse') public reverse: string) { }

  private get isReverse() {
    if (!this.reverse) return false;
    return this.reverse === 'true' ? true : false;
  }

  validate(c: AbstractControl): { [key: string]: any } {
    let v = c.value;
    let e = c.root.get(this.equal);

    if (e && v !== e.value && !this.isReverse) {
      return { equal: true };
    }

    if (e && e.value && v !== e.value && this.isReverse) {
      e.setErrors({ equal: true })
    }

    if (e && e.value && v === e.value && this.isReverse) {
      if (e.errors !== null) {
        delete e.errors['equal'];
        if (!Object.keys(e.errors).length) e.setErrors(null);
      }
    }

    return null;
  }

}
