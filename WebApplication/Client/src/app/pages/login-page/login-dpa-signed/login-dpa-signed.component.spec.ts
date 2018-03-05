import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LoginDpaSignedComponent } from './login-dpa-signed.component';

describe('LoginDpaSignedComponent', () => {
  let component: LoginDpaSignedComponent;
  let fixture: ComponentFixture<LoginDpaSignedComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LoginDpaSignedComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LoginDpaSignedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
