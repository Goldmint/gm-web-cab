import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LoginOntokenPageComponent } from './login-ontoken-page.component';

describe('LoginOntokenPageComponent', () => {
  let component: LoginOntokenPageComponent;
  let fixture: ComponentFixture<LoginOntokenPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LoginOntokenPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LoginOntokenPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
