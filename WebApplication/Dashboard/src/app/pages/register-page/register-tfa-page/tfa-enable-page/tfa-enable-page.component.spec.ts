import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TfaEnablePageComponent } from './tfa-enable-page.component';

describe('TfaEnablePageComponent', () => {
  let component: TfaEnablePageComponent;
  let fixture: ComponentFixture<TfaEnablePageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TfaEnablePageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TfaEnablePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
