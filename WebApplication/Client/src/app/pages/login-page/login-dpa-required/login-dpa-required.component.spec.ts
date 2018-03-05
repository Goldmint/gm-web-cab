import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LoginDpaRequiredComponent } from './login-dpa-required.component';

describe('LoginDpaRequiredComponent', () => {
  let component: LoginDpaRequiredComponent;
  let fixture: ComponentFixture<LoginDpaRequiredComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LoginDpaRequiredComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LoginDpaRequiredComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
