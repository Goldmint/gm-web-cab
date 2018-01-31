import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsTFAPageComponent } from './settings-tfa-page.component';

describe('SettingsTFAPageComponent', () => {
  let component: SettingsTFAPageComponent;
  let fixture: ComponentFixture<SettingsTFAPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SettingsTFAPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SettingsTFAPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
