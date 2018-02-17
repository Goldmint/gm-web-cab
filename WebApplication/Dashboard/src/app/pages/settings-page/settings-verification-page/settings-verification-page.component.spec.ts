import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsVerificationPageComponent } from './settings-verification-page.component';

describe('SettingsVerificationPageComponent', () => {
  let component: SettingsVerificationPageComponent;
  let fixture: ComponentFixture<SettingsVerificationPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SettingsVerificationPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SettingsVerificationPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
