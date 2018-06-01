import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output
} from '@angular/core';

@Component({
  selector: 'app-timer',
  templateUrl: './timer.component.html',
  styleUrls: ['./timer.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TimerComponent implements OnInit, AfterViewInit {

  @Input('time') expiresTime;
  @Output() endTimer: EventEmitter<any> = new EventEmitter();

  public timer: object = {};
  private deadline;
  private interval;

  constructor(private _cdRef: ChangeDetectorRef) { }

  ngOnInit() {
    this.deadline = new Date(this.expiresTime * 1000);
  }

  getTimeRemaining() {
    var t = Date.parse(this.deadline) - Date.parse(new Date().toString());
    var seconds = Math.floor((t / 1000) % 60);
    var minutes = Math.floor((t / 1000 / 60) % 60);
    var hours = Math.floor((t / (1000 * 60 * 60)) % 24);
    var days = Math.floor(t / (1000 * 60 * 60 * 24));
    return {
      'total': t,
      'days': days,
      'hours': hours,
      'minutes': minutes,
      'seconds': seconds
    };
  }

  initializeClock() {
    this.updateClock();
    this.interval = setInterval(this.updateClock.bind(this), 1000);
    this._cdRef.detectChanges();
  }

  updateClock() {
    var t = this.getTimeRemaining();

    this.timer['hours'] = ('0' + t.hours).slice(-2);
    this.timer['minutes'] = ('0' + t.minutes).slice(-2);
    this.timer['seconds'] = ('0' + t.seconds).slice(-2);

    if (t.total <= 0) {
      clearInterval(this.interval);
      this.endTimer.emit(true);
    }
    this._cdRef.detectChanges();
  }

  ngAfterViewInit() {
    this.initializeClock();
  }

  ngOnDestroy() {
    clearInterval(this.interval);
  }

}
