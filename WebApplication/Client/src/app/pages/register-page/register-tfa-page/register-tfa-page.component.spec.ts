import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RegisterTfaPageComponent } from './register-tfa-page.component';

describe('RegisterTfaPageComponent', () => {
  let component: RegisterTfaPageComponent;
  let fixture: ComponentFixture<RegisterTfaPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RegisterTfaPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RegisterTfaPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
