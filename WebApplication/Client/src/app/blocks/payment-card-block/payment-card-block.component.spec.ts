import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PaymentCardBlockComponent } from './payment-card-block.component';

describe('PaymentCardBlockComponent', () => {
  let component: PaymentCardBlockComponent;
  let fixture: ComponentFixture<PaymentCardBlockComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PaymentCardBlockComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PaymentCardBlockComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
