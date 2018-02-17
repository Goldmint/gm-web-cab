import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsActivityPageComponent } from './settings-activity-page.component';

describe('SettingsActivityPageComponent', () => {
  let component: SettingsActivityPageComponent;
  let fixture: ComponentFixture<SettingsActivityPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SettingsActivityPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SettingsActivityPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
