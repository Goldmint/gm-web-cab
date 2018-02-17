import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TfaNotActivatedPageComponent } from './tfa-not-activated-page.component';

describe('TfaNotActivatedPageComponent', () => {
  let component: TfaNotActivatedPageComponent;
  let fixture: ComponentFixture<TfaNotActivatedPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TfaNotActivatedPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TfaNotActivatedPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
