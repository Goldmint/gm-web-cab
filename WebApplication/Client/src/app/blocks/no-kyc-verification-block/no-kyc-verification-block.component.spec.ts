import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NoKycVerificationBlockComponent } from './no-kyc-verification-block.component';

describe('NoKycVerificationBlockComponent', () => {
  let component: NoKycVerificationBlockComponent;
  let fixture: ComponentFixture<NoKycVerificationBlockComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NoKycVerificationBlockComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NoKycVerificationBlockComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
