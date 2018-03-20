import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsFeesPageComponent } from './settings-fees-page.component';

describe('SettingsFeesPageComponent', () => {
  let component: SettingsFeesPageComponent;
  let fixture: ComponentFixture<SettingsFeesPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SettingsFeesPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SettingsFeesPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
