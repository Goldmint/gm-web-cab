import {Component, EventEmitter, OnDestroy, OnInit, Output} from '@angular/core';
import { DeviceDetectorService } from 'ngx-device-detector';
import {UserService} from "../../../services/user.service";
import {Subscription} from "rxjs/Subscription";

@Component({
  selector: 'app-launch-node-page',
  templateUrl: './launch-node-page.component.html',
  styleUrls: ['./launch-node-page.component.sass']
})
export class LaunchNodePageComponent implements OnInit, OnDestroy {

  @Output() migration = new EventEmitter<any>();

  public system: string = '';
  public locale: string;
  public osList = [
    {label: 'Windows', value: 'windows'},
    {label: 'Linux', value: 'linux'},
    {label: 'MacOS', value: 'mac'}
  ];
  public direction: string;

  public systemMap = {
    'windowsru': {
      video: '',
      text: 'https://github.com/Goldmint/sumus-docs/blob/master/instruction_for_win_RUS.pdf'
    },
    'windowsen': {
      video: '',
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
      video: '',
      text: ''
    },
    'linuxen': {
      video: '',
      text: ''
    }
  }

  private sub1: Subscription;

  constructor(
    private deviceService: DeviceDetectorService,
    private userService: UserService
  ) { }

  ngOnInit() {
    this.system = this.deviceService.getDeviceInfo().os;
    this.sub1 = this.userService.currentLocale.subscribe(locale => {
      this.locale = locale;
      this.direction = this.system + this.locale;
    });
    this.direction = this.system + this.locale;
  }

  chooseSystem(os: string) {
    this.system = os;
    this.direction = this.system + this.locale;
  }

  goToMigration() {
    this.migration.emit('migration');
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
  }

}
