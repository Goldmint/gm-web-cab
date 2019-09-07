import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TosVerificationPageComponent } from './tos-verification-page.component';

describe('TosVerificationPageComponent', () => {
  let component: TosVerificationPageComponent;
  let fixture: ComponentFixture<TosVerificationPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TosVerificationPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TosVerificationPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
