import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsCardsPageComponent } from './settings-cards-page.component';

describe('SettingsCardsPageComponent', () => {
  let component: SettingsCardsPageComponent;
  let fixture: ComponentFixture<SettingsCardsPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ SettingsCardsPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SettingsCardsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
