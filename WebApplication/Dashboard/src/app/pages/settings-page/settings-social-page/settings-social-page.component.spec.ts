import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsSocialPageComponent } from './settings-social-page.component';

describe('SettingsSocialPageComponent', () => {
  let component: SettingsSocialPageComponent;
  let fixture: ComponentFixture<SettingsSocialPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SettingsSocialPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SettingsSocialPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
