import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RegisterEmailTakenPageComponent } from './register-email-taken-page.component';

describe('RegisterEmailTakenPageComponent', () => {
  let component: RegisterEmailTakenPageComponent;
  let fixture: ComponentFixture<RegisterEmailTakenPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RegisterEmailTakenPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RegisterEmailTakenPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
