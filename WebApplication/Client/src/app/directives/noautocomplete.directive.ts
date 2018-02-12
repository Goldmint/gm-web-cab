import { Directive, ElementRef } from '@angular/core';

@Directive({
  selector: '[noAutoComplete]'
})
export class NoautocompleteDirective {

  constructor(el: ElementRef) {
    el.nativeElement.setAttribute("autocomplete", "off");
    el.nativeElement.setAttribute("autocorrect", "off");
    el.nativeElement.setAttribute("autocapitalize", "off");
    el.nativeElement.setAttribute("spellcheck", "false");
  }
}
