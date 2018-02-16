import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RegisterEmailConfirmedPageComponent } from './register-email-confirmed-page.component';

describe('RegisterEmailConfirmedPageComponent', () => {
  let component: RegisterEmailConfirmedPageComponent;
  let fixture: ComponentFixture<RegisterEmailConfirmedPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RegisterEmailConfirmedPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RegisterEmailConfirmedPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
