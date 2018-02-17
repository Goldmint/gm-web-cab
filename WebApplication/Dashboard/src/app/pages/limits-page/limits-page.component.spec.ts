import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LimitsPageComponent } from './limits-page.component';

describe('LimitsPageComponent', () => {
  let component: LimitsPageComponent;
  let fixture: ComponentFixture<LimitsPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LimitsPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LimitsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
