import {Component, EventEmitter, OnDestroy, OnInit, Output} from '@angular/core';
import { DeviceDetectorService } from 'ngx-device-detector';
import {UserService} from "../../../services/user.service";
import {Subscription} from "rxjs/Subscription";
import {DomSanitizer} from "@angular/platform-browser";

@Component({
  selector: 'app-launch-node-page',
  templateUrl: './launch-node-page.component.html',
  styleUrls: ['./launch-node-page.component.sass']
})
export class LaunchNodePageComponent implements OnInit, OnDestroy {

  @Output() migration = new EventEmitter<any>();

  public system: string = '';
  public locale: string;
  public videoUrl: any;
  public osList = [
    {label: 'Windows', value: 'windows'},
    {label: 'Linux', value: 'linux'}
    // {label: 'MacOS', value: 'mac'}
  ];
  public direction: string;

  public systemMap = {
    'windowsru': {
      video: '_9BUs5GKwU8',
      text: 'https://github.com/Goldmint/sumus-docs/blob/master/instruction_for_win_RUS.pdf'
    },
    'windowsen': {
      video: '4tLqYb_iD00',
      text: 'https://github.com/Goldmint/sumus-docs/blob/master/instruction_for_win_ENG.pdf'
    },
    'macru': {
      video: '',
      text: ''
    },
    'macen': {
      video: '',
      text: ''
    },
    'linuxru' : {
      video: 'elyLVU3Chpo',
      text: 'https://github.com/Goldmint/sumus-docs/blob/master/instruction_for_linux_RUS.pdf'
    },
    'linuxen': {
      video: 'Wi7831BnO8o',
      text: 'https://github.com/Goldmint/sumus-docs/blob/master/instruction_for_linux_ENG.pdf'
    }
  }

  private sub1: Subscription;

  constructor(
    private deviceService: DeviceDetectorService,
    private userService: UserService,
    public sanitizer: DomSanitizer
  ) { }

  ngOnInit() {
    this.system = this.deviceService.getDeviceInfo().os;
    this.sub1 = this.userService.currentLocale.subscribe(locale => {
      this.locale = locale;
      this.direction = this.system + this.locale;
      this.setVideoUrl();
    });
    this.direction = this.system + this.locale;
    this.setVideoUrl();
  }

  chooseSystem(os: string) {
    this.system = os;
    this.direction = this.system + this.locale;
    this.setVideoUrl();
  }

  setVideoUrl() {
    this.videoUrl = this.sanitizer.bypassSecurityTrustResourceUrl('https://www.youtube.com/embed/' + this.systemMap[this.direction].video);
  }

  goToMigration() {
    this.migration.emit('migration');
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }

}
