import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TelemetryPageComponent } from './telemetry-page.component';

describe('TelemetryPageComponent', () => {
  let component: TelemetryPageComponent;
  let fixture: ComponentFixture<TelemetryPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TelemetryPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TelemetryPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
