import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RegisterSuccessPageComponent } from './register-success-page.component';

describe('RegisterSuccessPageComponent', () => {
  let component: RegisterSuccessPageComponent;
  let fixture: ComponentFixture<RegisterSuccessPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RegisterSuccessPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RegisterSuccessPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
