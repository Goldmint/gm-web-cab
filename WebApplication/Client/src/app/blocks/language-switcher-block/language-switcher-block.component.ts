import { Component, OnInit, ViewEncapsulation/*, ChangeDetectionStrategy*/ } from '@angular/core';
import { TranslateService, LangChangeEvent } from '@ngx-translate/core';

import { UserService } from '../../services/user.service';

import { AppLanguages, AppDefaultLanguage } from '../../app.languages';

@Component({
  selector: 'language-switcher',
  templateUrl: './language-switcher-block.component.html',
  styleUrls: ['./language-switcher-block.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class LanguageSwitcherBlockComponent implements OnInit {

  public defaultLanguage: string = AppDefaultLanguage || 'en';

  public languages = {
    en: {
      name: 'English',
      icon: 'gb.png',
      locale: 'en'
    }
  }

  public constructor(
    private userService: UserService,
    public translate: TranslateService) {

    Object.assign(this.languages, AppLanguages);

    const codes = Object.keys(this.languages);

    // let userLanguage = localStorage.getItem('gmint_language')
    //   ? localStorage.getItem('gmint_language')
    //   : translate.getBrowserLang()
    // ;
    let userLanguage = 'en';

    userLanguage = userLanguage.match('^(' + codes.join('|') + ')$')
      ? userLanguage
      : this.defaultLanguage;

    translate.addLangs(codes);
    translate.setDefaultLang(this.defaultLanguage);
    translate.use(userLanguage);

    userService.setLocale(userLanguage);
  }

  ngOnInit() {
    this.translate.onLangChange.subscribe((event: LangChangeEvent) => {
      this.userService.setLocale(event.lang);
      localStorage.setItem('gmint_language', event.lang);
    });
  }

}
